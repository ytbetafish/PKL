namespace PCL;

public partial class Setup
{
    /// <summary>
    ///     KSP 改造后：原 Minecraft 版本/文件夹/内存类型的安装向导页面已剥离，
    ///     这些方法保留为空实现以兼容配置观察者调用。
    /// </summary>
    public static void LaunchInstanceSelect(string value)
    {
        Common.Log($"[Setup] 已选择版本：{value}");
    }

    public static void LaunchFolderSelect(string value)
    {
        Common.Log($"[Setup] 已选择游戏文件夹：{value.Replace("$", Common.ExePath)}");
    }

    public static void LaunchRamType(int type)
    {
        // KSP 改造后：MC 内存设置页面已剥离。
    }
}
