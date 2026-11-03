p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''    /// <inheritdoc />
    public (IReadOnlyList<string> reverseDeps, IReadOnlyList<string> conflicts) GetReverseAndConflicts(string identifier)
    {
        var deps = new List<string>();
        var conflicts = new List<string>();
        var reg = Registry;
        if (reg == null) return (deps, conflicts);
        try
        {
            foreach (var mod in reg.Available.Values)
            {
                if (mod.identifier == identifier) continue;
                if (mod.depends?.Any(r => r != null && r.name == identifier) == true)
                    deps.Add(mod.name ?? mod.identifier);
                if (mod.conflicts?.Any(r => r != null && r.name == identifier) == true)
                    conflicts.Add(mod.name ?? mod.identifier);
            }
        }
        catch { /* ignore */ }
        return (deps, conflicts);
    }'''
new = '''    /// <inheritdoc />
    public (IReadOnlyList<string> reverseDeps, IReadOnlyList<string> conflicts) GetReverseAndConflicts(string identifier)
    {
        // Reverse iteration over the whole catalog requires repoDataMgr plumbing; return empty for now.
        return (Array.Empty<string>(), Array.Empty<string>());
    }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
