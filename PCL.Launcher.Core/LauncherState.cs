namespace PCL;

/// <summary>
///     业务层共享的启动器运行状态。
///     KSP 改造后：原 Minecraft 实例选择状态已剥离，此处保留为空壳以承载后续 KSP 实例状态。
/// </summary>
public static class LauncherState
{
    /// <summary>当前选中的 KSP 安装路径（占位，后续接入真实实例管理）。</summary>
    public static string? SelectedKspDirectory { get; set; }
}
