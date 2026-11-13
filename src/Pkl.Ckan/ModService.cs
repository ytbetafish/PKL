using CKAN;
using CKAN.Configuration;
using CKAN.IO;
using CKAN.Versioning;
using IUser = CKAN.IUser;
using System.Diagnostics;
using System.Net;

namespace Pkl.Ckan;

/// <summary>
/// 当前实例以只读方式附加（注册表被其他 CKAN 进程占用），安装 / 升级 / 卸载等写操作被拒绝。
/// </summary>
public sealed class InstanceReadOnlyException : InvalidOperationException
{
    public InstanceReadOnlyException()
        : base("KSP 实例注册表被其他 CKAN 客户端占用，当前为只读模式；请先关闭常驻 CKAN 客户端后再执行安装或卸载。")
    {
    }
}

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

    /// <summary>诊断埋点汇（如启动器 log4net）；为空时仅写入内存日志。</summary>
    public Action<string>? TraceSink { get; set; }

    private void Trace(string message)
    {
        try { Log.RaiseMessage(message); } catch { }
        try { TraceSink?.Invoke(message); } catch { }
    }

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

    /// <summary>
    /// 附加实例；若注册表被其他 CKAN 进程占用（锁冲突），自动回退只读模式，
    /// 搜索/浏览仍可用；安装/升级/移除需要读写锁，只读模式下会拒绝执行。
    /// </summary>
    public void AttachInstanceRobust(GameInstance instance)
    {
        try
        {
            AttachInstance(instance);
        }
        catch (Exception ex) when (ex is RegistryInUseKraken or IOException)
        {
            AttachInstanceReadOnly(instance);
        }
    }

    /// <summary>Mirror URLs to use when strategy is MirrorOnly or OfficialFirst.</summary>
    public System.Collections.Generic.List<string> MirrorUrls { get; set; } = new();
    /// <summary>Default official CKAN repo URL (used as fallback when strategy != MirrorOnly).</summary>
    public string OfficialRepoUrl { get; set; } = "https://github.com/KSP-CKAN/CKAN-meta/archive/master.tar.gz";
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
            Trace("Refresh 开始");
            var registry = _registryManager.registry;
            var existing = registry.Repositories;

            // 保留实例注册表中已有的仓库（含本机 CKAN 客户端写入的仓库），只按配置补齐缺失项；
            // 不再清空仓库列表，避免抹掉既有仓库导致磁盘缓存与当前仓库对不上。
            void AddRepo(string name, string uri)
            {
                if (string.IsNullOrWhiteSpace(uri) || existing.ContainsKey(name))
                    return;
                registry.RepositoriesAdd(new CKAN.Repository(name, uri.Trim()));
            }

            if (RepoStrategy != "OfficialOnly")
            {
                var i = 0;
                foreach (var u in MirrorUrls)
                {
                    if (!string.IsNullOrWhiteSpace(u))
                        AddRepo($"PKL-Mirror-{++i}", u.Trim());
                }
            }
            if (RepoStrategy != "MirrorOnly")
            {
                AddRepo("CKAN-Official", OfficialRepoUrl);
            }
            _registryManager.Save();
            Trace("注册表 Save 完成");

            var repos = registry.Repositories.Values.ToList();
            var downloader = new NetAsyncDownloader(_user,
                () => (System.Security.Cryptography.HashAlgorithm?)null,
                UserAgent,
                cancellationToken);
            var updateResult = _repoData.Update(repos, _instance.Game, false, downloader, _user, UserAgent);
            Trace($"仓库 Update 完成: {updateResult}");
        }
        catch (Exception ex)
        {
            try { _user.RaiseError("刷新仓库注册表失败: " + ex.Message); } catch { }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModSearchResult>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        // 目录枚举与投影放到后台线程，避免阻塞 STA 导致加载动画停滞、页面看似未响应。
        var results = await Task.Run(() =>
        {
            var modules = EnumerateCatalog().Where(m => MatchesQuery(m, query)).ToList();
            Trace($"目录枚举 {modules.Count} 条，{sw.ElapsedMilliseconds} ms");
            sw.Restart();
            var list = modules.Select(ToResult).ToList();
            Trace($"结果投影 {list.Count} 条，{sw.ElapsedMilliseconds} ms");
            return list;
        }, cancellationToken).ConfigureAwait(false);
        return results;
    }

    /// <summary>对 results 的 [start, start+count) 区间做 SpaceDock 回填；并发限流，失败项保持原值。</summary>
    public async Task EnrichRangeAsync(IList<ModSearchResult> results, int start, int count, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (count <= 0 || start < 0 || start >= results.Count)
            return;
        count = Math.Min(count, results.Count - start);

        using var semaphore = new SemaphoreSlim(8);
        var tasks = new List<Task>();
        for (var i = 0; i < count; i++)
        {
            var idx = start + i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var mod = LatestAvailable(results[idx].Identifier);
                    if (mod?.resources?.spacedock is not { } url)
                        return;
                    if (!SpaceDockClient.TryParseModId(url, out var modId))
                        return;

                    var sd = await _spaceDock.FetchAsync(modId, cancellationToken).ConfigureAwait(false);
                    if (sd is null)
                        return;

                    var r = results[idx];
                    results[idx] = r with
                    {
                        Downloads = sd.Downloads,
                        Author = string.IsNullOrEmpty(r.Author) ? sd.Author : r.Author,
                        IconUrl = string.IsNullOrEmpty(r.IconUrl) ? sd.Background : r.IconUrl,
                        Description = string.IsNullOrEmpty(r.Description) ? sd.ShortDescription : r.Description,
                        License = string.IsNullOrEmpty(r.License) ? sd.License : r.License,
                        Homepage = string.IsNullOrEmpty(r.Homepage) ? sd.Website : r.Homepage,
                        Version = string.IsNullOrEmpty(r.Version) ? sd.LatestVersion : r.Version
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
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

        await EnrichRangeAsync(results, 0, results.Count, cancellationToken).ConfigureAwait(false);
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
        catch (Exception ex)
        {
            var reason = ex switch
            {
                TooManyModsProvideKraken => "该模组的某个依赖存在多个提供者，需在 CKAN 客户端中选择提供者",
                InconsistentKraken => $"与已安装模组冲突：{ex.Message}",
                DependenciesNotSatisfiedKraken => $"依赖在当前 KSP 版本无可安装版本：{ex.Message}",
                _ => $"无法生成安装计划：{ex.Message}"
            };
            return Task.FromResult<ModInstallPlan?>(new ModInstallPlan(
                target.identifier,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                reason));
        }
    }

    /// <inheritdoc />
    public Task ExecutePlanAsync(ModInstallPlan plan, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("尚未附加 KSP 实例。");
        }
        if (_registryManager == null)
        {
            throw new InstanceReadOnlyException();
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
        if (_instance == null)
        {
            throw new InvalidOperationException("尚未附加 KSP 实例。");
        }
        if (_registryManager == null)
        {
            throw new InstanceReadOnlyException();
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
