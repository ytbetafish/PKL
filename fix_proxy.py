p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''        _config = new JsonConfiguration(Path.Combine(dataDir, "config.json"));
        _repoData = new RepositoryDataManager(Path.Combine(dataDir, "repo-data"));
        _manager = new GameInstanceManager(_user, _config);

        // Apply HTTP proxy if configured (process-wide, since NetAsyncDownloader does not expose it).
        var proxy = PklSettings.Current.HttpProxy;
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            try
            {
                WebRequest.DefaultWebProxy = new WebProxy(proxy, true);
            }
            catch { /* ignore bad proxy */ }
        }
    }'''
new = '''        _config = new JsonConfiguration(Path.Combine(dataDir, "config.json"));
        _repoData = new RepositoryDataManager(Path.Combine(dataDir, "repo-data"));
        _manager = new GameInstanceManager(_user, _config);
    }

    /// <summary>
    /// Apply a process-wide HTTP proxy (NetAsyncDownloader does not take one in its ctor).
    /// </summary>
    public static void ApplyHttpProxy(string? proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy)) return;
        try { WebRequest.DefaultWebProxy = new WebProxy(proxy, true); } catch { }
    }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
