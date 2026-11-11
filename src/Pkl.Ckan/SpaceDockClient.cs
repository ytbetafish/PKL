using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Pkl.Ckan;

/// <summary>SpaceDock 模组详情（仅抽取启动器需要的字段）。</summary>
public sealed record SpaceDockMod(int Id, string Author, string? Background, int Downloads, string? ShortDescription);

/// <summary>
/// SpaceDock.info 只读客户端：按 CKAN 模块记录的 spacedock 链接补下载量、作者与横幅图。
/// 网络失败或模组不存在时降级为 null，不阻断 CKAN 注册表主流程；结果按 id 内存缓存。
/// </summary>
public sealed class SpaceDockClient
{
    private const string BaseUrl = "https://spacedock.info";

    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse("Pkl.Ckan/1.0") } }
    };

    private readonly ConcurrentDictionary<int, SpaceDockMod?> _cache = new();

    private sealed class ApiMod
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("background")] public string? Background { get; set; }
        [JsonPropertyName("downloads")] public int Downloads { get; set; }
        [JsonPropertyName("short_description")] public string? ShortDescription { get; set; }
    }

    /// <summary>从 SpaceDock 链接（/mod/&lt;id&gt;/&lt;slug&gt;）解析模组 id。</summary>
    public static bool TryParseModId(Uri? url, out int id)
    {
        id = 0;
        if (url is null)
            return false;
        var match = Regex.Match(url.AbsolutePath, @"/mod/(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out id);
    }

    /// <summary>拉取模组详情；失败或不存在时返回 null。</summary>
    public async Task<SpaceDockMod?> FetchAsync(int modId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(modId, out var cached))
            return cached;

        ApiMod? api;
        try
        {
            using var resp = await _http.GetAsync($"{BaseUrl}/api/mod/{modId}", cancellationToken)
                .ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _cache[modId] = null;
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            api = await JsonSerializer.DeserializeAsync<ApiMod>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            _cache[modId] = null;
            return null;
        }

        var result = api is null
            ? null
            : new SpaceDockMod(api.Id, api.Author ?? string.Empty, api.Background, api.Downloads, api.ShortDescription);
        _cache[modId] = result;
        return result;
    }
}