namespace PCL;

/// <summary>将业务导航桥接到 WPF 主窗口。KSP 改造后：MC 实例/Mod 详情页已剥离，保留空实现。</summary>
public sealed class WpfCompNavigationService : ICompNavigationService
{
    public void ShowCompDetail(object project)
    {
        // KSP：模组详情页尚未实现，预留占位
    }

    public void ShowTaskManager()
    {
        // KSP：任务管理页尚未实现，预留占位
    }

    public void ShowInstanceSelect()
    {
        // KSP：实例选择页尚未实现，预留占位
    }
}
