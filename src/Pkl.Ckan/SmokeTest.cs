namespace Pkl.Ckan;

/// <summary>
/// In-process smoke entry point. Not an executable (so we don't touch the .slnx): hosts call
/// <see cref="RunAsync"/> from their own test/boot code. Must never throw, even when KSP is not
/// installed on the machine.
/// </summary>
public static class SmokeTest
{
    /// <summary>
    /// Bootstrap the CKAN adapter and print how many KSP install candidates were detected.
    /// </summary>
    public static Task<int> RunAsync()
    {
        var lines = new List<string>();
        int candidateCount = 0;

        try
        {
            using var service = new ModService();
            lines.Add("ModService constructed OK (CKAN core loaded).");

            var probe = new KspInstanceService();
            var roots = probe.FindCandidateRoots();
            candidateCount = roots.Count;
            lines.Add($"Detected KSP candidate root(s): {candidateCount}");
            foreach (var r in roots)
            {
                var info = probe.Probe(r);
                lines.Add($"  - {r} (version: {info?.Version ?? "unknown"})");
            }
        }
        catch (Exception ex)
        {
            lines.Add($"Smoke test degraded gracefully: {ex.GetType().Name}: {ex.Message}");
        }

        foreach (var line in lines)
        {
            Console.WriteLine("[Pkl.Ckan] " + line);
        }

        return Task.FromResult(candidateCount);
    }
}
