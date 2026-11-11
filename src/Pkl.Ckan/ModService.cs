using CKAN;
using CKAN.Configuration;
using CKAN.IO;
using CKAN.Versioning;
using IUser = CKAN.IUser;
using System.Net;

namespace Pkl.Ckan;

/// <summary>
/// <see cref="IModService"/> implementation backed directly by CKAN.Core's public APIs.
/// The class is safe to construct even on a machine without KSP: all operations degrade to
/// empty/no-op results until an instance is attached.
/// </summary>
public sealed class ModService : IModService, IDisposable
{
    private const string UserAgent = "Pkl.Ckan/1.0";

    private readonly IUser _user;
    private readonly IConfiguration _config;
    private readonly RepositoryDataManager _repoData;
    private readonly GameInstanceManager _manager;

    private GameInstance? _instance;
    private RegistryManager? _registryManager;
    private Registry? _readonlyRegistry;
    private readonly SpaceDockClient _spaceDock = new();

    /// <summary>
    /// Create the adapter with a self-contained data directory under the user's temp folder,
    /// so it never collides with a system-wide CKAN install.
    /// </summary>
    public ModService()
        : this(null, null)
    {
    }

    /// <param name="user">CKAN user sink (defaults to a StringBuilder-backed user so we can show logs).</param>
    /// <param name="dataDir">Where CKAN config and cached repo data live; defaults to a temp dir.</param>
    public ModService(IUser? user = null, string? dataDir = null)
    {
        Log = user as StringBuilderUser ?? new StringBuilderUser();
        _user = Log;
        dataDir ??= Path.Combine(Path.GetTempPath(), "Pkl.Ckan");
        Directory.CreateDirectory(dataDir);
        _config = new JsonConfiguration(Path.Combine(dataDir, "config.json"));
        _repoData = new RepositoryDataManager(Path.Combine(dataDir, "repo-data"));
        _manager = new GameInstanceManager(_user, _config);
    }

    /// <summary>
    /// Apply process-wide network settings: HTTP proxy, default connection limit, idle timeout.
    /// (NetAsyncDownloader does not expose these in its ctor.)
    /// </summary>
    public static void ApplyNetworkSettings(string? proxy, int concurrency, int timeoutSeconds)
    {
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            try { WebRequest.DefaultWebProxy = new WebProxy(proxy, true); } catch { }
        }
        try { ServicePointManager.DefaultConnectionLimit = Math.Max(2, concurrency); } catch { }
        try { ServicePointManager.MaxServicePointIdleTime = Math.Max(5, timeoutSeconds) * 1000; } catch { }
    }

    /// <summary>Accumulated log sink (for the "view log" window).</summary>
    public StringBuilderUser Log { get; }

    /// <summary>The underlying CKAN game-instance manager (advanced use).</summary>
    public GameInstanceManager Manager => _manager;

    /// <summary>The currently attached instance, if any.</summary>
    public GameInstance? CurrentInstance => _instance;

    /// <summary>The registry used for read paths (read-write manager registry or the read-only one).</summary>
    private Registry? Registry => _registryManager?.registry ?? _readonlyRegistry;

    /// <summary>
    /// Attach a KSP instance (e.g. produced by <see cref="KspInstanceService"/>) and open its
    /// CKAN registry read-write. Required for install / upgrade / remove operations.
    /// </summary>
    public void AttachInstance(GameInstance instance)
    {
        _instance = instance;
        _readonlyRegistry = null;
        _manager.SetCurrentInstance(instance);
        _registryManager = RegistryManager.Instance(instance, _repoData);
    }

    /// <summary>
    /// Attach a KSP instance and open its CKAN registry read-only (no lock, no writes into the
    /// game's CKAN folder). Suitable for listing / search / planning.
    /// </summary>
    public void AttachInstanceReadOnly(GameInstance instance)
    {
        _instance = instance;
        _registryManager?.Dispose();
        _registryManager = null;
        _readonlyRegistry = RegistryManager.ReadOnlyRegistry(instance, _repoData);
    }

    /// <summary>Mirror URLs to use when strategy is MirrorOnly or OfficialFirst.</summary>
    public System.Collections.Generic.List<string> MirrorUrls { get; set; } = new();
    /// <summary>Default official CKAN repo URL (used as fallback when strategy != MirrorOnly).</summary>
    public string OfficialRepoUrl { get; set; } = "https://archive.ksp-ckan.net/CKAN-meta.zip";
    /// <summary>One of "MirrorOnly" / "OfficialFirst" / "OfficialOnly".</summary>
    public string RepoStrategy { get; set; } = "OfficialFirst";

    /// <inheritdoc />
    public Task RefreshMasterRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            // Rebuild repo list according to strategy. Order matters: earlier repos win,
            // later repos fill in what earlier ones don't have.
            _registryManager.registry.RepositoriesClear();

            if (RepoStrategy != "OfficialOnly")
            {
                foreach (var u in MirrorUrls)
                {
                    if (!string.IsNullOrWhiteSpace(u))
                        try { _registryManager.registry.RepositoriesAdd(new CKAN.Repository("PKL-Mirror", u.Trim())); } catch { }
                }
            }
            if (RepoStrategy != "MirrorOnly")
            {
                try { _registryManager.registry.RepositoriesAdd(new CKAN.Repository("CKAN-Official", OfficialRepoUrl)); } catch { }
            }
            _registryManager.Save();

            var repos = _registryManager.registry.Repositories.Values.ToList();
            var downloader = new NetAsyncDownloader(_user,
                () => (System.Security.Cryptography.HashAlgorithm?)null,
                UserAgent,
                cancellationToken);
            _repoData.Update(repos, _instance.Game, false, downloader, _user, UserAgent);
        }
        catch
        {
            // Offline / transient errors: keep the previously cached registry.
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModSearchResult>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var results = EnumerateCatalog()
            .Where(m => MatchesQuery(m, query))
            .Select(ToResult)
            .ToList();
        await EnrichFromSpaceDock(results, cancellationToken).ConfigureAwait(false);
        return results;
    }

    /// <summary>用 SpaceDock 详情补下载量、作者、横幅图与简介；CKAN 已有值不覆盖。</summary>
    private async Task EnrichFromSpaceDock(List<ModSearchResult> results, CancellationToken cancellationToken)
    {
        for (var i = 0; i < results.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var mod = LatestAvailable(results[i].Identifier);
            if (mod?.resources?.spacedock is not { } url)
                continue;
            if (!SpaceDockClient.TryParseModId(url, out var modId))
                continue;

            var sd = await _spaceDock.FetchAsync(modId, cancellationToken).ConfigureAwait(false);
            if (sd is null)
                continue;

            var r = results[i];
            results[i] = r with
            {
                Downloads = sd.Downloads,
                Author = string.IsNullOrEmpty(r.Author) ? sd.Author : r.Author,
                IconUrl = string.IsNullOrEmpty(r.IconUrl) ? sd.Background : r.IconUrl,
                Description = string.IsNullOrEmpty(r.Description) ? sd.ShortDescription : r.Description
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModSearchResult>> ListInstalledAsync(CancellationToken cancellationToken = default)
    {
        var reg = Registry;
        if (_instance == null || reg == null)
        {
            return Array.Empty<ModSearchResult>();
        }

        var installed = reg.Installed();
        var results = new List<ModSearchResult>();
        foreach (var (identifier, _) in installed)
        {
            var mod = LatestAvailable(identifier);
            results.Add(ToResult(mod, identifier, installed));
        }

        await EnrichFromSpaceDock(results, cancellationToken).ConfigureAwait(false);
        return results;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModVersionInfo>> GetVersionsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var reg = Registry;
        var list = new List<ModVersionInfo>();
        if (_instance == null || reg == null || string.IsNullOrWhiteSpace(identifier))
        {
            return Task.FromResult<IReadOnlyList<ModVersionInfo>>(list);
        }

        List<CkanModule> modules;
        try
        {
            modules = reg.AvailableByIdentifier(identifier).ToList();
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<ModVersionInfo>>(list);
        }

        var criteria = _instance.VersionCriteria();
        foreach (var m in modules.OrderByDescending(m => m.version))
        {
            bool compatible;
            try { compatible = m.IsCompatible(criteria); } catch { compatible = true; }

            var urls = m.download?
                .Select(u => u.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            var gameVersions = new List<string>();
            if (m.ksp_version_min is not null) gameVersions.Add(m.ksp_version_min.ToString());
            if (m.ksp_version_max is not null) gameVersions.Add(m.ksp_version_max.ToString());
            else if (m.ksp_version is not null) gameVersions.Add(m.ksp_version.ToString());
            // 未声明兼容版本时兜底为不限版本，保证版本卡片能进入分组。
            if (gameVersions.Count == 0) gameVersions.Add("*");

            list.Add(new ModVersionInfo(
                m.identifier,
                m.version.ToString(),
                m.release_date?.ToString("yyyy-MM-dd"),
                urls,
                gameVersions,
                compatible));
        }

        return Task.FromResult<IReadOnlyList<ModVersionInfo>>(list);
    }

    /// <inheritdoc />
    public Task<ModInstallPlan?> GetInstallPlanAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var reg = Registry;
        if (_instance == null || reg == null)
        {
            return Task.FromResult<ModInstallPlan?>(null);
        }

        var target = LatestAvailable(identifier);
        if (target == null)
        {
            return Task.FromResult<ModInstallPlan?>(null);
        }

        try
        {
            var opts = RelationshipResolverOptions.DependsOnlyOpts(_instance.StabilityToleranceConfig);
            var resolver = new RelationshipResolver(
                modulesToInstall: new[] { target },
                modulesToRemove: null,
                options: opts,
                registry: reg,
                game: _instance.Game,
                versionCrit: _instance.VersionCriteria());

            var installed = reg.Installed();
            var toInstall = new List<string>();
            var toUpgrade = new List<string>();

            foreach (var mod in resolver.ModList())
            {
                if (installed.TryGetValue(mod.identifier, out var current))
                {
                    if (mod.version.CompareTo(current) > 0)
                    {
                        toUpgrade.Add(mod.identifier);
                    }
                }
                else
                {
                    toInstall.Add(mod.identifier);
                }
            }

            return Task.FromResult<ModInstallPlan?>(new ModInstallPlan(
                target.identifier,
                toInstall,
                toUpgrade,
                Array.Empty<string>()));
        }
        catch
        {
            return Task.FromResult<ModInstallPlan?>(null);
        }
    }

    /// <inheritdoc />
    public Task ExecutePlanAsync(ModInstallPlan plan, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            throw new InvalidOperationException("Instance not attached read-write; call AttachInstance first.");
        }

        var opts = RelationshipResolverOptions.DependsOnlyOpts(_instance.StabilityToleranceConfig);
        Directory.CreateDirectory(_instance.DownloadCacheDir);
        var cache = new NetModuleCache(_instance.DownloadCacheDir);
        var installer = new ModuleInstaller(_instance, cache, _config, _user, cancellationToken);

        var desired = plan.ToInstall.Concat(plan.ToUpgrade)
            .Select(LatestAvailable)
            .Where(m => m != null)
            .Select(m => m!)
            .ToList();

        if (desired.Count > 0)
        {
            HashSet<string>? possibleConfigOnlyDirs = null;
            installer.InstallList(desired, opts, _registryManager, ref possibleConfigOnlyDirs,
                userAgent: UserAgent, ConfirmPrompt: false);
        }

        if (plan.ToRemove.Count > 0)
        {
            HashSet<string>? possibleConfigOnlyDirs = null;
            installer.UninstallList(plan.ToRemove, ref possibleConfigOnlyDirs, _registryManager, ConfirmPrompt: false);
        }

        _registryManager.Save();

        progress?.Report($"Plan for '{plan.Target}' applied.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UninstallAsync(string identifier, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            throw new InvalidOperationException("Instance not attached read-write; call AttachInstance first.");
        }

        Directory.CreateDirectory(_instance.DownloadCacheDir);
        var cache = new NetModuleCache(_instance.DownloadCacheDir);
        var installer = new ModuleInstaller(_instance, cache, _config, _user, cancellationToken);

        HashSet<string>? possibleConfigOnlyDirs = null;
        installer.UninstallList(new[] { identifier }, ref possibleConfigOnlyDirs, _registryManager, ConfirmPrompt: false);

        _registryManager.Save();
        progress?.Report($"Uninstalled '{identifier}'.");
        return Task.CompletedTask;
    }

    // ---- internals -----------------------------------------------------------

    private IEnumerable<CkanModule> EnumerateCatalog()
    {
        var reg = Registry;
        if (_instance == null || reg == null)
        {
            yield break;
        }

        var repos = reg.Repositories.Values.ToList();
        foreach (var avail in _repoData.GetAllAvailableModules(repos))
        {
            CkanModule? best = null;
            foreach (var m in avail.AllAvailable())
            {
                if (best == null || m.version.CompareTo(best.version) > 0)
                {
                    best = m;
                }
            }

            if (best != null)
            {
                yield return best;
            }
        }
    }

    private CkanModule? LatestAvailable(string identifier)
    {
        var reg = Registry;
        if (reg == null)
        {
            return null;
        }

        try
        {
            CkanModule? best = null;
            foreach (var m in reg.AvailableByIdentifier(identifier))
            {
                if (best == null || m.version.CompareTo(best.version) > 0)
                {
                    best = m;
                }
            }

            return best;
        }
        catch
        {
            // Installed entries without repo metadata (DLL-provided, redistributables, etc.)
            // make CKAN throw ModuleNotFoundKraken; treat them as "not in catalog".
            return null;
        }
    }

    private static bool MatchesQuery(CkanModule mod, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var q = query.Trim();
        return mod.identifier.Contains(q, StringComparison.OrdinalIgnoreCase)
            || mod.name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (mod.author != null && mod.author.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    private ModSearchResult ToResult(CkanModule? mod)
    {
        var installed = Registry?.Installed() ?? new Dictionary<string, ModuleVersion>();
        return ToResult(mod, mod?.identifier ?? string.Empty, installed);
    }

    private ModSearchResult ToResult(CkanModule? mod, string identifier, IReadOnlyDictionary<string, ModuleVersion> installed)
    {
        bool isInstalled = installed.TryGetValue(identifier, out var installedVersion);
        bool hasUpdate = isInstalled && mod != null && mod.version.CompareTo(installedVersion) > 0;

        bool compatible = true;
        if (mod != null && _instance != null)
        {
            try { compatible = mod.IsCompatible(_instance.VersionCriteria()); }
            catch { /* leave default */ }
        }

        // CKAN 模块元数据不含图标 URL（ResourcesDescriptor 无 icon 字段），图标留待 SpaceDock 兜底。
        string? iconUrl = null;

        return new ModSearchResult(
            identifier,
            mod?.name ?? identifier,
            mod?.description ?? mod?.@abstract,
            mod?.author is { Count: > 0 } a ? string.Join(", ", a) : null,
            mod?.license is { Count: > 0 } l ? string.Join(", ", l) : null,
            mod?.version?.ToString() ?? installedVersion?.ToString(),
            mod?.resources?.homepage?.ToString(),
            isInstalled,
            hasUpdate,
            compatible,
            iconUrl);
    }

    /// <inheritdoc />
    public (IReadOnlyList<string> reverseDeps, IReadOnlyList<string> conflicts) GetReverseAndConflicts(string identifier)
    {
        var deps = new List<string>();
        var conflicts = new List<string>();
        if (_instance == null) return (deps, conflicts);
        try
        {
            var repos = Registry?.Repositories?.Values ?? Enumerable.Empty<CKAN.Repository>();
            foreach (var am in _repoData.GetAllAvailableModules(repos))
            {
                foreach (var mod in am.AllAvailable())
                {
                    if (mod.identifier == identifier) continue;
                    if (mod.depends?.Any(r => (r as CKAN.ModuleRelationshipDescriptor)?.name == identifier) == true)
                        deps.Add(mod.name ?? mod.identifier);
                    if (mod.conflicts?.Any(r => (r as CKAN.ModuleRelationshipDescriptor)?.name == identifier) == true)
                        conflicts.Add(mod.name ?? mod.identifier);
                }
            }
        }
        catch { /* ignore */ }
        return (deps.Distinct().ToList(), conflicts.Distinct().ToList());
    }

    public void Dispose()
    {
        try { _registryManager?.Dispose(); } catch { /* best effort */ }
        try { _manager.Dispose(); } catch { /* best effort */ }
    }
}
