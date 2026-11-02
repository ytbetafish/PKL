using System.IO;
using System.Text.Json;

namespace PCL;

/// <summary>
///     PKL 自己的设置（KSP 路径 + 翻译服务选项），持久化为 exe 同目录下 pkl-settings.json。
///     不走 PCL 的 source-generator Config 系统，避免动 PCL.Core。
/// </summary>
public static class PklSettings
{
    private static readonly string _path =
        Path.Combine(LauncherPaths.ExecutableDirectory, "pkl-settings.json");

    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    public static SettingsData Current { get; private set; } = new();

    public class SettingsData
    {
        public string KspRootPath { get; set; } = @"D:\Kerbal Space Program";
        public string TranslateBaseUrl { get; set; } = "https://api.deepseek.com/v1";
        public string TranslateApiKey { get; set; } = "";
        public string TranslateModel { get; set; } = "deepseek-chat";
    }

    static PklSettings()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Current = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch
        {
            Current = new SettingsData();
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, _jsonOpts));
        }
        catch
        {
            // 写盘失败不致命。
        }
    }
}
