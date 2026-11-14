namespace Pkl.Ckan;

/// <summary>
/// A detected KSP install on disk.
/// </summary>
/// <param name="RootPath">Absolute path of the KSP root (the folder containing KSP_x64.exe).</param>
/// <param name="Version">Detected KSP version string, or null if it could not be determined.</param>
public sealed record KspInstanceInfo(string RootPath, string? Version);

/// <summary>
/// A row returned by mod search / listing.
/// </summary>
public sealed record ModSearchResult(
    string Identifier,
    string Name,
    string? Description,
    string? Author,
    string? License,
    string? Version,
    string? Homepage,
    bool IsInstalled,
    bool HasUpdate,
    bool IsCompatible = true,
    string? IconUrl = null,
    int Downloads = 0,
    /// <summary>本模组所依赖（作为前置）的模组显示名列表。</summary>
    IReadOnlyList<string>? Depends = null,
    /// <summary>依赖本模组（把它作为前置）的模组显示名列表。</summary>
    IReadOnlyList<string>? ReverseDeps = null,
    /// <summary>与本模组冲突的模组显示名列表。</summary>
    IReadOnlyList<string>? Conflicts = null);

/// <summary>
/// The install/upgrade/remove plan computed by CKAN's dependency resolver.
/// When <see cref="FailureReason"/> is non-null the plan could not be produced; the lists are empty.
/// </summary>
public sealed record ModInstallPlan(
    string Target,
    IReadOnlyList<string> ToInstall,
    IReadOnlyList<string> ToUpgrade,
    IReadOnlyList<string> ToRemove,
    string? FailureReason = null);
/// <summary>
/// A single published version of a catalogued mod.
/// </summary>
public sealed record ModVersionInfo(
    string Identifier,
    string Version,
    string? ReleaseDate,
    IReadOnlyList<string> DownloadUrls,
    IReadOnlyList<string> GameVersions,
    bool IsCompatible);
