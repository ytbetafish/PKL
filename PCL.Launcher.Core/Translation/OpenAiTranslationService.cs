using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PCL.Translation;

/// <summary>
///     基于 OpenAI 兼容 /chat/completions 接口的翻译实现。
/// </summary>
public class OpenAiTranslationService : ITranslationService
{
    private readonly TranslationOptions _options;
    private static readonly HttpClient _http = new();

    public OpenAiTranslationService(TranslationOptions options)
    {
        _options = options;
    }

    public async Task<string?> TranslateAsync(string text, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var url = _options.BaseUrl.TrimEnd('/') + "/chat/completions";
        var systemPrompt =
            $"You are a translation engine. Translate the user's text to {targetLang}. " +
            "Return ONLY the translated text, no explanations, no quotes, no markdown.";

        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = text
                }
            },
            ["temperature"] = 0.2
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var resp = await _http.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadFromJsonAsync<JsonNode>().ConfigureAwait(false);
            var result = body?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch
        {
            return null;
        }
    }
}
