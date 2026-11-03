p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''    /// <summary>
    /// Apply a process-wide HTTP proxy (NetAsyncDownloader does not take one in its ctor).
    /// </summary>
    public static void ApplyHttpProxy(string? proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy)) return;
        try { WebRequest.DefaultWebProxy = new WebProxy(proxy, true); } catch { }
    }'''
new = '''    /// <summary>
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
    }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
