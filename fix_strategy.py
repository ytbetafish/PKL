p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''    /// <summary>
    /// Optional mirror repo URL to use instead of CKAN's official repos. Set by caller.
    /// </summary>
    public string? MirrorUrl { get; set; }

    /// <inheritdoc />
    public Task RefreshMasterRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            // If a mirror URL is configured, replace the official repos with our single mirror.
            if (!string.IsNullOrWhiteSpace(MirrorUrl))
            {
                try
                {
                    _registryManager.registry.RepositoriesClear();
                    _registryManager.registry.RepositoriesAdd(new CKAN.Repository("PKL-Mirror", MirrorUrl));
                    _registryManager.Save();
                }
                catch { /* best effort */ }
            }

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
    }'''

new = '''    /// <summary>Mirror URLs to use when strategy is MirrorOnly or OfficialFirst.</summary>
    public System.Collections.Generic.List<string> MirrorUrls { get; set; } = new();
    /// <summary>One of "MirrorOnly" / "OfficialFirst" / "OfficialOnly".</summary>
    public string RepoStrategy { get; set; } = "MirrorOnly";

    /// <inheritdoc />
    public Task RefreshMasterRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (_instance == null || _registryManager == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            // Configure repositories based on strategy.
            var chosen = new List<CKAN.Repository>();
            if (RepoStrategy != "OfficialOnly" && MirrorUrls.Count > 0)
            {
                foreach (var u in MirrorUrls)
                {
                    if (!string.IsNullOrWhiteSpace(u))
                        chosen.Add(new CKAN.Repository("PKL-Mirror", u.Trim()));
                }
            }
            if (RepoStrategy != "MirrorOnly")
            {
                // Keep / add official repos (CKAN default). We don't have a hardcoded official URL,
                // so leave existing official repos in place when no mirrors are chosen.
                if (chosen.Count == 0)
                {
                    // OfficialOnly: leave default repos untouched.
                }
                else
                {
                    // OfficialFirst: add mirrors after official repos.
                    foreach (var r in chosen)
                    {
                        try { _registryManager.registry.RepositoriesAdd(r); } catch { }
                    }
                    _registryManager.Save();
                    goto run;
                }
            }
            else
            {
                // MirrorOnly: wipe everything, use only mirrors.
                try
                {
                    _registryManager.registry.RepositoriesClear();
                    foreach (var r in chosen) _registryManager.registry.RepositoriesAdd(r);
                    _registryManager.Save();
                }
                catch { }
            }

            run:
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
    }'''

assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
