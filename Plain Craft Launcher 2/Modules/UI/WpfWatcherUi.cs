using PCL.Core.Platform;

namespace PCL;

/// <summary>将游戏监视器通知桥接到 WPF 主窗口。KSP 改造后仅保留窗口退出/显示骨架。</summary>
public sealed class WpfWatcherUi : IWatcherUi
{
    public void RefreshShutdownButton()
    {
        // KSP：暂无游戏进程监视，保留空实现
    }

    public void ExitApplication()
    {
        PlatformServices.UiDispatcher?.Post(() =>
            Launcher.frmMain?.EndProgram(false));
    }

    public void ShowApplication()
    {
        PlatformServices.UiDispatcher?.Post(() =>
        {
            if (Launcher.frmMain is { } main) main.Hidden = false;
        });
    }

    public void ShowFeedback()
    {
        Common.FeedbackInfo();
    }

    public void ShowHint(string text, NotificationType type = NotificationType.Info)
    {
        DialogCompat.ShowHint(text, type);
    }
}

/// <summary>媒体服务桥接。KSP 改造后：背景音乐/视频背景已剥离，保留空实现。</summary>
public sealed class WpfMediaService : IMediaService
{
    public void PauseMusic() { }

    public void ResumeMusic() { }

    public void SetVideoGaming(bool gaming) { }
}
