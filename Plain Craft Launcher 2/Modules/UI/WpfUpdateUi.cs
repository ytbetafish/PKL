using PCL.Core.Platform;

namespace PCL;

/// <summary>将更新业务通知桥接到 WPF 主窗口。KSP 改造后：设置页更新标签已剥离，保留对话框/提示与重启按钮。</summary>
public sealed class WpfUpdateUi : IUpdateUi
{
    public int ShowMessageBox(
        string caption,
        string? title = null,
        string? button1 = null,
        string? button2 = "",
        string? button3 = "",
        bool isWarn = false,
        Action? button1Action = null,
        Action? button2Action = null,
        Action? button3Action = null)
    {
        return DialogCompat.ShowMessageBox(
            caption,
            title,
            button1,
            button2,
            button3,
            isWarn,
            button1Action: button1Action,
            button2Action: button2Action,
            button3Action: button3Action);
    }

    public void ShowHint(string text, NotificationType type = NotificationType.Info)
    {
        DialogCompat.ShowHint(text, type);
    }

    public void NavigateSetupUpdate()
    {
        // KSP：设置页更新标签尚未实现，预留占位
    }

    public void ShowUpdateRestartButton(string tooltip)
    {
        PlatformServices.UiDispatcher?.Post(() =>
        {
            if (Launcher.frmMain is not { } main) return;
            LocalizedText.TryBind(main.BtnExtraUpdateRestart, "ToolTip", () => tooltip);
            main.BtnExtraUpdateRestart.ShowRefresh();
            main.BtnExtraUpdateRestart.Ribble();
        });
    }

    public void RefreshDownloadButton()
    {
        PlatformServices.UiDispatcher?.Post(() =>
        {
            Launcher.frmMain?.BtnExtraDownload.ShowRefresh();
        });
    }

    public void UpdateSetupButton(string text, bool enabled)
    {
        // KSP：更新按钮尚未在设置页实现，预留占位
    }

    public void ExitApplication(bool restart)
    {
        PlatformServices.UiDispatcher?.Post(() =>
            Launcher.frmMain?.EndProgram(false, restart));
    }
}

/// <summary>公告按钮事件桥接。KSP 改造后：主页自定义事件系统已剥离，保留空实现。</summary>
public sealed class WpfAnnouncementActions : IAnnouncementActionHandler
{
    public void Execute(string command, string? parameter)
    {
        // KSP：公告交互动作尚未实现，预留占位
    }
}
