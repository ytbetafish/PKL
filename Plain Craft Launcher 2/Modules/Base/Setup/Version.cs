namespace PCL;

public partial class Setup
{
    /// <summary>
    ///     KSP 改造后：原 Minecraft 实例设置页面（内存/服务器登录）已剥离，
    ///     保留空实现以兼容配置观察者调用。
    /// </summary>
    public static void VersionRamType(int type)
    {
        // KSP 改造后：MC 实例内存设置页面已剥离。
    }

    public static void VersionServerLogin(int type)
    {
        // KSP 改造后：MC 实例服务器登录设置已剥离。
    }
}
