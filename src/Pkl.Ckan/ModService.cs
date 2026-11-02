using CKAN;
using CKAN.Configuration;
using CKAN.IO;
using CKAN.Versioning;
using IUser = CKAN.IUser;

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

    /// <summary>
    /// Create the adapter with a self-contained data directory under the user's temp folder,
    /// so it never collides with a system-wide CKAN install.
    /// </summary>
    public ModService()
        : this(new NullUser(), null)
    {
    }

    /// <param name="user">CKAN user sink (defaults to a null/no-op user).</param>
    /// <param name="dataDir">Where CKAN config and cached repo data live; defaults to a temp dir.</param>
    public ModService(IUser? user = null, string? dataDir = null)
    {
        _user = user ?? new NullUser();
        dataDir ??= Path.Combine(Path.GetTempPath(), "Pkl.Ckan");
        Directory.CreateDirectory(dataDir);
        _config = new JsonConfiguration(Path.Combine(dataDir, "config.json"));
        _repoData = new RepositoryDataManager(Path.Combine(dataDir, "repo-data"));
        _manager = new GameInstanceManager(_user, _config);
    }

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

    /// <inheritdoc />
    public Task RefreshMasterRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            return Task.CompletedTask;
        }

        try
        {
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
    public Task<IReadOnlyList<ModSearchResult>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var results = EnumerateCatalog()
            .Where(m => MatchesQuery(m, query))
            .Select(ToResult)
            .ToList();
        return Task.FromResult<IReadOnlyList<ModSearchResult>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModSearchResult>> ListInstalledAsync(CancellationToken cancellationToken = default)
    {
        var reg = Registry;
        if (_instance == null || reg == null)
        {
            return Task.FromResult<IReadOnlyList<ModSearchResult>>(Array.Empty<ModSearchResult>());
        }

        var installed = reg.Installed();
        var results = new List<ModSearchResult>();
        foreach (var (identifier, _) in installed)
        {
            var mod = LatestAvailable(identifier);
            results.Add(ToResult(mod, identifier, installed));
        }

        return Task.FromResult<IReadOnlyList<ModSearchResult>>(results);
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

        return new ModSearchResult(
            identifier,
            mod?.name ?? identifier,
            mod?.description ?? mod?.@abstract,
            mod?.author is { Count: > 0 } a ? string.Join(", ", a) : null,
            mod?.license is { Count: > 0 } l ? string.Join(", ", l) : null,
            mod?.version?.ToString() ?? installedVersion?.ToString(),
            mod?.resources?.homepage?.ToString(),
            isInstalled,
            hasUpdate);
    }

    public void Dispose()
    {
        try { _registryManager?.Dispose(); } catch { /* best effort */ }
        try { _manager.Dispose(); } catch { /* best effort */ }
    }
}
