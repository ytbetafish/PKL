namespace PCL.Translation;

/// <summary>
///     翻译服务的可配置项。base_url / api_key / model 可在设置 JSON 中修改。
/// </summary>
public class TranslationOptions
{
    /// <summary>
    ///     OpenAI 兼容接口的 Base URL，例如 https://api.openai.com/v1 或 https://api.deepseek.com/v1。
    ///     留空则不启用翻译。
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";

    /// <summary>
    ///     API Key（Bearer Token）。
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    ///     模型名，例如 deepseek-chat、gpt-4o-mini。
    /// </summary>
    public string Model { get; set; } = "deepseek-chat";
}
