using System;
using System.IO;
using System.Text.Json;

namespace PCL;

/// <summary>
///     PKL 自己的设置（KSP 路径 + 翻译服务选项）。
///     不走 PCL 的 source-generator Config 系统，避免动 PCL.Core。
///     持久化到 %APPDATA%\PKL\pkl-settings.json，保证 exe 位于只读目录（如 Program Files、
///     解压到受保护位置的 Release zip）时也能读写，避免 Access denied。
///     若旧版本曾把配置写在 exe 同目录，启动时自动迁移一次。
/// </summary>
public static class PklSettings
{
    private static readonly string _path = ResolvePath();

    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    public static SettingsData Current { get; private set; } = new();

    public class SettingsData
    {
        public string KspRootPath { get; set; } = @"D:\Kerbal Space Program";
        public string TranslateBaseUrl { get; set; } = "https://api.deepseek.com/v1";
        public string TranslateApiKey { get; set; } = "";
        public string TranslateModel { get; set; } = "deepseek-chat";

        public string CkanDataDir { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PKL", "Ckan");
        public string CkanMasterRepoUrl { get; set; } = "https://archive.ksp-ckan.net/CKAN-meta.zip";
        public string CkanStabilityTolerance { get; set; } = "Stable";
    }

    private static string ResolvePath()
    {
        // 首选 %APPDATA%\PKL（每个用户、必然可写）。
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PKL");
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, "pkl-settings.json");

        // 迁移旧版 exe 同目录配置。
        try
        {
            var legacy = Path.Combine(LauncherPaths.ExecutableDirectory, "pkl-settings.json");
            if (!File.Exists(target) && File.Exists(legacy))
                File.Copy(legacy, target, true);
        }
        catch
        {
            // 迁移失败不致命，继续使用默认/现有路径。
        }
        return target;
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
            // 原子写：先写临时文件再替换，避免写一半或被占用导致的损坏/权限异常。
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, _jsonOpts));
            if (File.Exists(_path))
                File.Replace(tmp, _path, _path + ".bak");
            else
                File.Move(tmp, _path);
        }
        catch
        {
            // 写盘失败不致命，但不静默吞掉——至少留个痕迹。
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "pkl-settings-save-error.log"),
                $"[{DateTime.Now}] save to {_path} failed{Environment.NewLine}"); } catch { }
        }
    }
}
