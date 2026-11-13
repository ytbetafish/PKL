using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using Pkl.Ckan;

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

    /// <summary>模组详情页实例。</summary>
    public static PageModDetail? frmDownloadCompDetail;

    private static ModService? _ckanService;
    private static bool _ckanInitialized;

    /// <summary>CKAN 模组管理服务（懒加载单例）。</summary>
    public static IModService Ckan
    {
        get
        {
            if (_ckanService is null)
                InitializeCkan();
            return _ckanService!;
        }
    }

    /// <summary>定位本机 KSP 实例并附加到 CKAN 服务；失败时回退为无实例的空服务，不阻断启动。</summary>
    private static void InitializeCkan()
    {
        if (_ckanInitialized)
            return;
        _ckanInitialized = true;
        try
        {
            var settings = PklSettings.Current;
            ModService.ApplyNetworkSettings(
                settings.HttpProxy,
                settings.DownloadConcurrency,
                settings.HttpTimeoutSeconds);
            var service = new ModService(dataDir: settings.CkanDataDir);
            service.MirrorUrls = new System.Collections.Generic.List<string>(settings.CkanMirrorUrls);
            service.OfficialRepoUrl = settings.CkanMasterRepoUrl;
            service.RepoStrategy = settings.CkanRepoStrategy;
            service.TraceSink = msg => Common.Log("[CKAN] " + msg);
            var detector = new KspInstanceService();
            var root = detector.FindCandidateRoots().FirstOrDefault();
            if (root is not null)
            {
                var instance = detector.Load(root);
                service.AttachInstanceRobust(instance);
            }
            _ckanService = service;
            // 后台刷新注册表，不阻塞 UI（必须 Task.Run：RefreshMasterRegistryAsync 为同步方法，直接调用会在 STA 线程阻塞联网下载仓库）
            _ = System.Threading.Tasks.Task.Run(() => service.RefreshMasterRegistryAsync());
        }
        catch
        {
            // CKAN 初始化失败不影响启动；列表在服务不可用时显示空态
            _ckanService ??= new ModService();
        }
    }

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

    /// <summary>
    ///     申请一个可用于任务缓存的临时文件夹，以 \ 结尾。这些文件夹无需进行后续清理。
    ///     若所有缓存位置均没有权限，会抛出异常。
    /// </summary>
    /// <param name="requireNonSpace">是否要求路径不包含空格。</param>
    public static string RequestTaskTempFolder(bool requireNonSpace = false)
    {
        TryClearTaskTemp();

        try
        {
            var tempFolder =
                $@"{Common.pathTemp}TaskTemp\{Common.GetUuid()}-{RandomUtils.NextInt(0, 1000000)}\";

            if (!requireNonSpace || !tempFolder.Contains(' '))
            {
                Directory.CreateDirectory(tempFolder);
                Common.CheckPermissionWithException(tempFolder);
                return tempFolder;
            }
        }
        catch
        {
            // ignored
        }

        var fallbackFolder = Path.Combine(
            SystemPaths.DriveLetter,
            "ProgramData",
            "PCL",
            "TaskTemp",
            $"{Common.GetUuid()}-{RandomUtils.NextInt(0, 1000000)}");

        Directory.CreateDirectory(fallbackFolder);
        Common.CheckPermission(fallbackFolder);

        return fallbackFolder;
    }
}
