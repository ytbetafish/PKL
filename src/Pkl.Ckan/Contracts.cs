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
    bool HasUpdate);

/// <summary>
/// The install/upgrade/remove plan computed by CKAN's dependency resolver.
/// </summary>
public sealed record ModInstallPlan(
    string Target,
    IReadOnlyList<string> ToInstall,
    IReadOnlyList<string> ToUpgrade,
    IReadOnlyList<string> ToRemove);
