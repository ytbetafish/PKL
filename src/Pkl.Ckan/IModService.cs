namespace Pkl.Ckan;

/// <summary>
/// High-level mod-management surface built on top of CKAN.Core.
/// </summary>
public interface IModService
{
    /// <summary>
    /// Refresh the master mod registry from the configured CKAN repositories (network).
    /// Safe to call even when offline: failures are swallowed and the cached registry is kept.
    /// </summary>
    Task RefreshMasterRegistryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search the available mod catalog by identifier / name / author substring.
    /// </summary>
    Task<IReadOnlyList<ModSearchResult>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    /// <summary>
    /// List mods currently installed in the attached KSP instance.
    /// </summary>
    Task<IReadOnlyList<ModSearchResult>> ListInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Use CKAN's dependency resolver to compute the install/upgrade/remove plan for a mod.
    /// Returns null if the instance has no registry or the identifier is unknown.
    /// </summary>
    Task<ModInstallPlan?> GetInstallPlanAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a previously computed plan (downloads + installs/upgrades/removes).
    /// </summary>
    Task ExecutePlanAsync(ModInstallPlan plan, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall an installed mod (plus any orphaned dependencies).
    /// </summary>
    Task UninstallAsync(string identifier, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find mods that depend on (or conflict with) the given identifier.
    /// Returns (reverseDeps, conflicts) lists of mod identifiers.
    /// </summary>
    (IReadOnlyList<string> reverseDeps, IReadOnlyList<string> conflicts) GetReverseAndConflicts(string identifier);
}
