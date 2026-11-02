namespace PCL.Translation;

/// <summary>
///     简介/文本翻译服务。从 PCL-CE 原 Mod 简介翻译（PCL.Launcher.Core\Comp\ProjectProps.cs 的
///     GetChineseDescriptionAsync）抽取为独立可复用接口。
///     默认实现为 OpenAI 兼容的 /chat/completions 接口，base_url / api_key / model 均可在设置中配置。
/// </summary>
public interface ITranslationService
{
    /// <summary>
    ///     将给定文本翻译为目标语言。
    /// </summary>
    /// <param name="text">待翻译原文。</param>
    /// <param name="targetLang">目标语言，如 "zh-CN"、"en"。</param>
    /// <returns>译文；失败时返回 null。</returns>
    Task<string?> TranslateAsync(string text, string targetLang);
}
