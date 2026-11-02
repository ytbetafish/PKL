using CKAN;
using CKAN.Configuration;
using CKAN.Games;

namespace Pkl.Ckan;

/// <summary>
/// Detects KSP installations on disk and loads them through CKAN's <see cref="GameInstance"/>
/// machinery. Detection combines CKAN's own Steam/registry scan with a manual probe of common
/// Steam library locations plus caller-supplied paths.
/// </summary>
public sealed class KspInstanceService
{
    private readonly IUser _user;
    private readonly string _dataDir;
    private readonly IGame _kspGame = KnownGames.GameByShortName("KSP")
        ?? throw new InvalidOperationException("CKAN could not resolve the KSP game definition.");

    public KspInstanceService(IUser? user = null, string? dataDir = null)
    {
        _user = user ?? new NullUser();
        _dataDir = dataDir ?? Path.Combine(Path.GetTempPath(), "Pkl.Ckan");
    }

    /// <summary>
    /// Heuristic check: does <paramref name="dir"/> look like a KSP root (KSP_x64.exe / KSP.exe
    /// plus a GameData folder)?
    /// </summary>
    public static bool IsKspRoot(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            return false;
        }

        bool hasExe = File.Exists(Path.Combine(dir, "KSP_x64.exe"))
                   || File.Exists(Path.Combine(dir, "KSP.exe"));
        bool hasGameData = Directory.Exists(Path.Combine(dir, "GameData"));
        return hasExe && hasGameData;
    }

    /// <summary>
    /// Enumerate candidate KSP root directories. This never throws: detection failures (no
    /// Steam, no registry access) simply yield fewer candidates.
    /// </summary>
    public IReadOnlyList<string> FindCandidateRoots()
    {
        var candidates = new List<string>();

        // 1) CKAN's own detection (Steam registry + libraryfolder vdfs).
        try
        {
            Directory.CreateDirectory(_dataDir);
            var config = new JsonConfiguration(Path.Combine(_dataDir, "config.json"));
            using var mgr = new GameInstanceManager(_user, config);
            foreach (var inst in mgr.FindDefaultInstances())
            {
                if (!string.IsNullOrWhiteSpace(inst.GameDir) && IsKspRoot(inst.GameDir))
                {
                    candidates.Add(inst.GameDir);
                }
            }
        }
        catch
        {
            // Detection is best-effort; fall through to manual paths.
        }

        // 2) Common Steam install locations.
        foreach (var steamRoot in CommonSteamRoots())
        {
            TryAdd(candidates, Path.Combine(steamRoot, "steamapps", "common", "Kerbal Space Program"));
            // KSP2: "Kerbal Space Program 2"
            TryAdd(candidates, Path.Combine(steamRoot, "steamapps", "common", "Kerbal Space Program 2"));
        }

        // 3) Non-Steam drives that are common for KSP.
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
        {
            try
            {
                TryAdd(candidates, Path.Combine(drive.Name, "KSP"));
                TryAdd(candidates, Path.Combine(drive.Name, "Games", "Kerbal Space Program"));
            }
            catch
            {
                // ignore inaccessible drives
            }
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Probe a directory and return its <see cref="KspInstanceInfo"/> if it looks like a KSP root.
    /// </summary>
    public KspInstanceInfo? Probe(string rootPath)
    {
        if (!IsKspRoot(rootPath))
        {
            return null;
        }

        string? version = null;
        try
        {
            var inst = Load(rootPath, Path.GetFileName(rootPath));
            version = inst.Version()?.ToString();
        }
        catch
        {
            // Version detection is best-effort.
        }

        return new KspInstanceInfo(rootPath, version);
    }

    /// <summary>
    /// Build a CKAN <see cref="GameInstance"/> for an already-validated KSP root.
    /// </summary>
    public GameInstance Load(string rootPath, string? name = null)
    {
        if (!IsKspRoot(rootPath))
        {
            throw new DirectoryNotFoundException($"Not a KSP root: {rootPath}");
        }

        return new GameInstance(_kspGame, rootPath, name ?? Path.GetFileName(rootPath));
    }

    private static IEnumerable<string> CommonSteamRoots()
    {
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrEmpty(progFiles))
        {
            yield return Path.Combine(progFiles, "Steam");
        }

        var progFiles64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(progFiles64))
        {
            yield return Path.Combine(progFiles64, "Steam");
        }
    }

    private static void TryAdd(List<string> list, string path)
    {
        if (IsKspRoot(path))
        {
            list.Add(path);
        }
    }
}
