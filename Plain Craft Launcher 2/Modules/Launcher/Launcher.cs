using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PCL;

/// <summary>
///     启动器全局静态状态。
///     KSP 改造后：原持有全部 MC 页面实例引用的逻辑已剥离。
///     此处保留主窗口/启动画面引用、跨模块系统能力，以及为兼容残留调用而保留的空实现字段。
/// </summary>
public static partial class Launcher
{
    /// <summary>主窗口实例。</summary>
    public static FormMain? frmMain;

    /// <summary>启动画面实例。</summary>
    public static SplashScreen? frmStart;

    /// <summary>
    ///     KSP 改造后：主页自定义事件系统已随 Event 目录一并删除，
    ///     各 UI 控件残留的事件上报调用统一为空实现。
    /// </summary>
    public static void RaiseCustomEvent(DependencyObject control)
    {
        // 已剥离主页自定义事件系统，不再执行任何事件派发。
    }

    /// <summary>愚人节彩蛋开关。KSP 改造后保留字段以兼容残留调用，默认关闭。</summary>
    public static bool isAprilEnabled;

    /// <summary>愚人节彩蛋距离。KSP 改造后保留字段以兼容残留调用。</summary>
    public static int _aprilDistance;

    /// <summary>愚人节放弃标记。KSP 改造后保留字段以兼容残留调用。</summary>
    public static bool isAprilGiveup;

    /// <summary>主时钟拖拽控件。KSP 改造后保留字段以兼容残留调用。</summary>
    public static MySlider? dragControl;

    /// <summary>
    ///     KSP 改造后：原主时钟（任务栏进度/愚人节彩蛋）已随 MC 导航剥离，
    ///     此方法保留为空实现以兼容启动调用。
    /// </summary>
    public static void TimerMainStart()
    {
        // 主时钟已剥离。
    }

    /// <summary>
    ///     KSP 改造后：清理任务临时目录的逻辑已随 MC 下载编排剥离，保留空实现。
    /// </summary>
    public static void TryClearTaskTemp()
    {
        // 已剥离。
    }

    /// <summary>
    ///     KSP 改造后：原 MC 启动后把窗口置顶的系统调用已剥离，保留空实现。
    /// </summary>
    public static void SetForegroundWindow()
    {
        // 已剥离。
    }

    /// <summary>
    ///     将特定程序设置为使用高性能显卡启动。失败时静默忽略（不影响启动）。
    /// </summary>
    public static void SetGpuPreference(string executable, bool wantHighPerformance = true)
    {
        try
        {
            const string gpuPreferenceRegistryKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
            const string highPerformancePreference = "GpuPreference=2;";
            const string defaultPreference = "GpuPreference=0;";

            using var readOnlyKey = Registry.CurrentUser.OpenSubKey(gpuPreferenceRegistryKey, false);
            var currentValue = readOnlyKey?.GetValue(executable)?.ToString();
            if (currentValue == (wantHighPerformance ? highPerformancePreference : defaultPreference))
                return;

            using var writeKey = Registry.CurrentUser.OpenSubKey(gpuPreferenceRegistryKey, true);
            writeKey?.SetValue(executable, wantHighPerformance ? highPerformancePreference : defaultPreference);
        }
        catch
        {
            // GPU 偏好设置失败不影响启动
        }
    }
}
