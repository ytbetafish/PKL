using System.IO;
using System.Text.Json.Nodes;
using Pkl.Ckan;

namespace PCL;

/// <summary>
/// CKAN 模组模型与启动器 Comp 模型之间的适配层。
/// 只做字段映射与 JsonObject 组装，不改动 <see cref="Comp.CompProject"/> / <see cref="Comp.CompFile"/>
/// 的既有模型字段。
/// </summary>
public static class CkanCompAdapter
{
    /// <summary>来源标识（对应 CompProject 的 DataSource；非 CurseForge，故 FromCurseForge 为 false）。</summary>
    private const string DataSource = "CKAN";

    /// <summary>
    /// 将一条 CKAN 搜索结果映射为启动器 Comp 工程模型。
    /// </summary>
    public static Comp.CompProject ToProject(ModSearchResult mod)
    {
        var json = new JsonObject
        {
            ["DataSource"] = DataSource,
            ["Type"] = (int)Comp.CompType.Mod,
            ["Slug"] = mod.Identifier,
            ["Id"] = mod.Identifier,
            ["RawName"] = mod.Name,
            ["Description"] = mod.Description ?? string.Empty,
            ["Website"] = mod.Homepage ?? string.Empty,
            // ModSearchResult 不携带下载量计数，置 0；后续可由 SpaceDock 兜底补充。
            ["DownloadCount"] = 0,
            ["Tags"] = new JsonArray(),
        };

        // 可空字段仅在有值时写入，避免空引用进入 JSON。
        if (!string.IsNullOrEmpty(mod.IconUrl))
            json["LogoUrl"] = mod.IconUrl;

        return new Comp.CompProject(json);
    }

    /// <summary>
    /// 将一组 CKAN 搜索结果批量映射为 Comp 工程模型。
    /// </summary>
    public static List<Comp.CompProject> ToProjects(IReadOnlyList<ModSearchResult> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        return mods.Select(ToProject).ToList();
    }

    /// <summary>
    /// 将一条 CKAN 搜索结果映射为单版本 CompFile（详情页文件列表占位）。
    /// 版本数组与下载链接需在详情页阶段从 CKAN CkanModule 细化。
    /// </summary>
    public static Comp.CompFile ToFile(ModSearchResult mod)
    {
        var json = new JsonObject
        {
            ["FromCurseForge"] = false,
            ["Id"] = mod.Identifier,
            ["ProjectId"] = mod.Identifier,
            ["DisplayName"] = mod.Name,
            ["Version"] = mod.Version ?? string.Empty,
            ["FileName"] = string.Empty,
            ["DownloadUrls"] = new JsonArray(),
            // 0 = Release
            ["Status"] = 0,
        };

        return new Comp.CompFile(json, Comp.CompType.Mod);
    }

    /// <summary>
    /// 将一条 CKAN 版本信息映射为 CompFile（详情页版本文件列表）。
    /// </summary>
    public static Comp.CompFile ToFile(ModVersionInfo version)
    {
        var json = new JsonObject
        {
            ["FromCurseForge"] = false,
            ["Id"] = version.Version,
            ["ProjectId"] = version.Identifier,
            ["DisplayName"] = version.Version,
            ["Version"] = version.Version,
            ["FileName"] = PickFileName(version.DownloadUrls, version.Version),
            ["DownloadUrls"] = new JsonArray(version.DownloadUrls.Select(u => (JsonNode)u).ToArray()),
            ["GameVersions"] = new JsonArray(version.GameVersions.Select(g => (JsonNode)g).ToArray()),
            // 0 = Release
            ["Status"] = 0,
        };

        if (!string.IsNullOrEmpty(version.ReleaseDate) && DateTime.TryParse(version.ReleaseDate, out var releaseDate))
            json["ReleaseDate"] = releaseDate;

        return new Comp.CompFile(json, Comp.CompType.Mod);
    }

    /// <summary>
    /// 将一组 CKAN 版本信息批量映射为 CompFile 列表。
    /// </summary>
    public static List<Comp.CompFile> ToFiles(IReadOnlyList<ModVersionInfo> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);
        return versions.Select(ToFile).ToList();
    }

    private static string PickFileName(IReadOnlyList<string> downloadUrls, string fallback)
    {
        if (downloadUrls.Count == 0)
            return fallback;
        try
        {
            return Path.GetFileName(new Uri(downloadUrls[0]).LocalPath);
        }
        catch
        {
            return fallback;
        }
    }
}
