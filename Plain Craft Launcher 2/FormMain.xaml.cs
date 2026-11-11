using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.UI.Hints;
using PCL.Core.Logging;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.UI.Dialogs;
using PCL.Core.UI.Theme;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public partial class FormMain
{
    private static PageSettingsLeft? settingsLeft;
    private static string settingsNav = "ksp";
    // 愚人节鼠标位置
    public MouseEventArgs lastMouseArg;

    private void FormMain_MouseMove(object sender, MouseEventArgs e)
    {
        lastMouseArg = e;
    }

    #region 基础

    // 更新日志
    private void ShowUpdateLog()
    {
        Common.RunInNewThread(() =>
        {
            var changelogFile = $"{Common.pathTemp}CEUpdateLog.md";
            string changelog;
            if (File.Exists(changelogFile))
                changelog = Common.ReadFile(changelogFile);
            else
                changelog = Lang.Text("Main.UpdateLog.Empty");
            if ((DialogService.Instance.Show(new MarkdownDialogRequest
            {
                Title = Lang.Text("Main.UpdateLog.Title", Common.VersionBranchName, Common.VersionBaseName),
                Content = changelog,
                Buttons = DialogButtons.Create("Common.Action.Confirm", "Main.UpdateLog.FullChangelog"),
                ForceWait = true
            }).Result?.Button ?? 0) ==
                2) Common.OpenWebsite("https://github.com/PCL-Community/PCL2-CE/releases");
        }, "UpdateLog Output");
    }

    // 窗口加载
    private bool isWindowLoadFinished;
    private readonly DragHelper _helper = new();

    public FormMain()
    {
        Common.applicationStartTick = TimeUtils.GetTimeTick();
        // 刷新主题
        // ThemeCheckAll(False)
        // ThemeRefreshColor()
        ThemeService.ColorModeChanged += (_, _) => ThemeManager.ThemeRefresh();
        ThemeService.ColorThemeChanged += theme => ThemeManager.ThemeRefresh((int)theme);
        // 窗体参数初始化
        Launcher.frmMain = this;
        Navigation = new NavigationService(this);
        // 版本号改变
        var lastVersion = States.System.LastVersion;
        if (lastVersion < Common.VersionCode)
        {
            // 重新询问是否启用遥测数据收集
            if (lastVersion <= 511)
            {
                if (!Config.System.TelemetryConfig.IsDefault() && Config.System.Telemetry)
                {
                    Config.System.TelemetryConfig.Reset();
                    Common.Log("[Start] 遥测策略变更：由旧版本升级到含新版遥测的版本，已重置遥测设置");
                }
            }
            // 触发升级
            UpgradeSub(lastVersion);
        }
        else if (lastVersion > Common.VersionCode)
            // 触发降级
            DowngradeSub(lastVersion);

        _ = Config.Preference.Theme.ThemeSelected;
        // 注册拖拽事件（不能直接加 Handles，否则没用；#6340）
        AddHandler(DragDrop.DragEnterEvent, new DragEventHandler(HandleDrag), true);
        AddHandler(DragDrop.DragOverEvent, new DragEventHandler(HandleDrag), true);
        // 注册 Hint 事件
        // 加载 UI
        InitializeComponent();
        Opacity = 0d;
        try
        {
            //             Height = States.UI.WindowHeight;
            //             Width = States.UI.WindowWidth;
        }
        catch (Exception ex) // 修复 #2019
        {
            Common.Log(
                ex,
                "读取窗口默认大小失败",
                Common.LogLevel.Hint,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
            Height = MinHeight + 100d;
            Width = MinWidth + 100d;
        }

        // 管理员权限下文件拖拽
        if (ProcessInterop.IsAdmin())
        {
            Common.Log("[Start] PCL 当前正以管理员权限运行");
            SourceInitialized += (_, _) =>
            {
                var windowInterop = new WindowInteropHelper(this);
                _helper.HwndSource = HwndSource.FromHwnd(windowInterop.Handle);
                _helper.AddHook();
            };
            Closing += (_, _) => _helper.RemoveHook();
            _helper.DragDrop += (_, _) => FileDrag(_helper.DropFilePaths);
        }

        // KSP 改造后：左侧栏隐藏，右侧直接托管 KSP 页面。
        var placeholderLeft = new MyPageLeft();
        PanMainLeft.Child = placeholderLeft;
        pageLeft = placeholderLeft;
        PanMainLeft.Child = new PageLaunchLeft();
        PanMainRight.Child = new Pages.PageHome();
        // 调试模式提醒
        if (Common.modeDebug)
            HintService.Show(Hints.Info(Lang.Text("Main.DebugMode.Hint")));
        // KSP 改造后：MC 版本文件夹加载池已剥离。

        Common.Log("[Start] 第二阶段加载用时：" + (TimeUtils.GetTimeTick() - Common.applicationStartTick) + " ms");
        // 注册生命周期状态事件
        Lifecycle.When(LifecycleState.WindowCreated, FormMain_Loaded);
    }

    private void FormMain_Loaded() // (sender As Object, e As RoutedEventArgs) Handles Me.Loaded
    {
        FormMain_SizeChanged();
        Common.applicationStartTick = TimeUtils.GetTimeTick();
        Common.frmHandle = new WindowInteropHelper(this).Handle;
        // 读取设置
        // KSP: PageSetupUI.BackgroundRefresh / Music.MusicRefreshPlay 已随 MC 设置页与音乐模块剥离。
        // 扩展按钮
        BtnExtraUpdateRestart.showCheck = BtnExtraUpdateRestart_ShowCheck;
        BtnExtraDownload.showCheck = BtnExtraDownload_ShowCheck;
        BtnExtraBack.showCheck = BtnExtraBack_ShowCheck;
        BtnExtraApril.showCheck = BtnExtraApril_ShowCheck;
        BtnExtraShutdown.showCheck = BtnExtraShutdown_ShowCheck;
        BtnExtraLog.showCheck = BtnExtraLog_ShowCheck;
        BtnExtraApril.ShowRefresh();
        // 初始化尺寸改变
        if (!Config.Preference.LockWindowSize)
            AddResizer();
        else
            RemoveResizer();
        // PKL 品牌：隐藏原版 PCL 矢量 logo + CE 角标，显示 PKL 文字
        try
        {
            ShapeTitleLogo.Visibility = Visibility.Collapsed;
            ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
            CELogo.Visibility = Visibility.Collapsed;
            LabTitleLogo.Text = "PKL";
            LabTitleLogo.Visibility = Visibility.Visible;
        }
        catch { }
        // PLC 彩蛋
        if (RandomUtils.NextInt(1, 1000) == 233)
            ShapeTitleLogo.Data = (Geometry)new GeometryConverter().ConvertFromString(
                "M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5");
        // 加载窗口

        ThemeManager.ThemeRefresh();
        Setup.ApplyAll();
        Lifecycle.CurrentApplication.SetResource(
            "BlurSamplingRate",
            Config.Preference.Blur.SamplingRate * 0.01d);
        Lifecycle.CurrentApplication.SetResource("BlurType", Config.Preference.Blur.KernelType);
        if (Config.Preference.Blur.IsEnabled)
            Lifecycle.CurrentApplication.SetResource(
                "BlurRadius",
                Config.Preference.Blur.Radius * 1.0d);
        else
            Lifecycle.CurrentApplication.SetResource("BlurRadius", 0.0d);

        // #If DEBUG Then
        // MinHeight = 50
        // MinWidth = 50
        // #End If
        Topmost = false;
        if (Launcher.frmStart is not null)
            Launcher.frmStart.Close(new TimeSpan(0, 0, 0, 0, (int)Math.Round(400d / Animations.aniSpeed)));
        // 更改窗口
        // Top = (GetWPFSize(My.Computer.Screen.WorkingArea.Height) - Height) / 2
        // Left = (GetWPFSize(My.Computer.Screen.WorkingArea.Width) - Width) / 2
        isSizeSaveable = true;
        ShowWindowToTop();
        var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        hwndSource.AddHook(WndProc);
        Animations.AniStart(new[]
        {
            Animations.AaCode(() => Animations.AniEndControl(), 50),
            Animations.AaOpacity(this, Config.Preference.Theme.WindowOpacity / 1000d + 0.4d - Opacity, 250, 100),
            Animations.AaDouble(i => TransformPos.Y += (double)i, -TransformPos.Y, 600,
                100, new Animations.AniEaseOutBack(Animations.AniEasePower.Weak)),
            Animations.AaDouble(i => TransformRotate.Angle += (double)i,
                -TransformRotate.Angle, 500, 100, new Animations.AniEaseOutBack(Animations.AniEasePower.Weak)),
            Animations.AaCode(() =>
            {
                RenderTransform = null;
                // 增量动画累加存在浮点误差，残留的微小旋转/位移会导致文字无法对齐像素而发虚
                PanRoot.RenderTransform = null;
                isWindowLoadFinished = true;
                Common.Log(
                    $"[System] DPI：{Common.Dpi}，系统版本：{Environment.OSVersion.VersionString}，PCL 位置：{Basics.ExecutablePath}");
            }, after: true)
        }, "Form Show");
        // Timer 启动
        Animations.AniStart();
        Launcher.TimerMainStart();
        // 特殊版本提示
        Common.RunInNewThread(() =>
        {
            // 特殊版本提示
            try
            {


#if false // DEBUG || DEBUGCI

                if (Environment.GetEnvironmentVariable("PCL_DISABLE_DEBUG_HINT") is null)
                {

#if DEBUG
                    var hint = Lang.Text("Main.SpecialVersion.DebugHint");
#else
                    var hint = Lang.Text("Main.SpecialVersion.CiHint");
#endif

                    DialogService.Instance.Show(new TextDialogRequest
                    {
                        Title = Lang.Text("Main.SpecialVersion.Title"),
                        Content = $"{hint}{"\r\n"}{"\r\n"}{Lang.Text("Main.SpecialVersion.HideHintNotice")}",
                        Buttons = DialogButtons.From(DialogButton.Create(Lang.Text("Main.SpecialVersion.IUnderstand")), DialogButton.Create(Lang.Text("Main.SpecialVersion.OpenDownloadPageAndExit"), () =>
                        {
                            Common.OpenWebsite("https://github.com/PCL-Community/PCL2-CE/releases/latest");
                            EndProgram(false);
                        })),
                        IsWarn = true,
                        ForceWait = true
                    });
                }


#endif
                // EULA 提示
                if (!States.System.LauncherEula)
                    switch ((DialogService.Instance.Show(new TextDialogRequest
                    {
                        Title = Lang.Text("Main.Eula.Title"),
                        Content = Lang.Text("Main.Eula.Message"),
                        Buttons = DialogButtons.From(DialogButton.Create(Lang.Text("Common.Action.Agree")), DialogButton.Create(Lang.Text("Common.Action.Decline")), DialogButton.Create(Lang.Text("Main.Eula.View"), () => Common.OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW"))),
                        ForceWait = true
                    }).Result?.Button ?? 0))
                    {
                        case 1:
                            {
                                States.System.LauncherEula = true;
                                break;
                            }
                        case 2:
                            {
                                EndProgram(false);
                                break;
                            }
                    }

                // 遥测提示
                if (Config.System.TelemetryConfig.IsDefault())
                {
                    var selection = (DialogService.Instance.Show(new TextDialogRequest
                    {
                        Title = Lang.Text("Main.Telemetry.Title"),
                        Content = Lang.Text("Main.Telemetry.Message"),
                        Buttons = DialogButtons.AgreeDecline,
                        ForceWait = true
                    }).Result?.Button ?? 0);
                    Config.System.TelemetryConfig.SetValue(selection == 1, forceNewValue: true);
                }
                // 启动加载器池
                try
                {
                    RunCountSub();
                    UpdateManager.serverLoader.Start(1);
                }
                catch (Exception ex)
                {
                    Common.Log(
                        ex,
                        "初始化加载池运行失败",
                        Common.LogLevel.Feedback,
                        userSummary: Lang.Text("Main.Error.OperationFailed"));
                }

                HardwareInfo.GetHardwareInfo();
            }
            catch (Exception ex)
            {
                Common.Log(
                    ex,
                    "初始弹窗提示运行失败",
                    Common.LogLevel.Feedback,
                    userSummary: Lang.Text("Main.Error.OperationFailed"));
            }
        }, "Start Loader", ThreadPriority.BelowNormal);

        Common.Log($"[Start] 第三阶段加载用时：{TimeUtils.GetTimeTick() - Common.applicationStartTick} ms");
    }

    // 根据打开次数触发的事件
    private void RunCountSub()
    {
        States.System.StartupCount += 1;
    }

    // 升级与降级事件
    private void UpgradeSub(int lastVersionCode)
    {
        Common.Log("[Start] 版本号从 " + lastVersionCode + " 升高到 " + Common.VersionCode);
        States.System.LastVersion = Common.VersionCode;
        // 检查有记录的最高版本号
        int lowerVersionCode;
#if BETA
        lowerVersionCode = States.System.LastBetaVersion;
        if (lowerVersionCode < Common.VersionCode)
        {
            States.System.LastBetaVersion = Common.VersionCode;
            Common.Log($"[Start] 最高版本号从 {lowerVersionCode} 升高到 {Common.VersionCode}");
        }
#else
        lowerVersionCode = States.System.LastAlphaVersion;
        if (lowerVersionCode < Common.VersionCode)
        {
            States.System.LastAlphaVersion = Common.VersionCode;
            Common.Log($"[Start] 最高版本号从 {lowerVersionCode} 升高到 {Common.VersionCode}");
        }
#endif
        // 被移除的窗口设置选项 (Commit 3161488 2026/1/23)
        if ((int)Config.Launch.GameWindowMode == 5)
            Config.Launch.GameWindowMode = GameWindowSizeMode.Default;

        // 更新后展示社区版提示
        // UpdateManager.ShowCeAnnounce(); // 去掉: 启动时弹出的PCL CE版本说明对话框
        // 输出更新日志
        if (lastVersionCode <= 0)
            return;
        if (lowerVersionCode >= Common.VersionCode)
            return;
        ShowUpdateLog();

        // 重置自定义主页配置
        if (lastVersionCode < 521 && Config.Preference.Homepage.SelectedPreset >= 3)
        {
            Config.Preference.Homepage.SelectedPreset = 0;
            DialogService.Instance.Show(new TextDialogRequest
            {
                Title = Lang.Text("Main.HomepageReset.Title"),
                Content = Lang.Text("Main.HomepageReset.Content"),
                Buttons = DialogButtons.Ok,
                ForceWait = true
            });
        }
    }

    private void DowngradeSub(int lastVersionCode)
    {
        Common.Log("[Start] 版本号从 " + lastVersionCode + " 降低到 " + Common.VersionCode);
        States.System.LastVersion = Common.VersionCode;
    }

    #endregion

    #region 自定义窗口

    private bool canResize = true;

    // 重写窗口边缘判定以使 DWM 自带的 resizer 行为看起来比较正常
    private nint _SizeWndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // 窗口活动常量
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        // WPF 尺寸的 offset
        const int offsetWpf = 6;
        const int hitWidthWpf = 5;

        // 过滤非 WM_NCHITTEST 事件
        if (msg != WM_NCHITTEST)
            return nint.Zero;

        // 提取鼠标坐标
        // 没妈的 VB 强转还得检查一下幻想的妈是不是还活着
        var mouseBytes = BitConverter.GetBytes(lParam.ToInt64());
        var xMouse = BitConverter.ToInt16(mouseBytes, 0);
        var yMouse = BitConverter.ToInt16(mouseBytes, 2);

        // 获取窗口参数
        var windowRect = WindowInterop.GetWindowRectangle(hWnd);
        var windowBounds = windowRect.ToWindowBounds();

        // 判断鼠标是否在窗口范围内
        var isInWindow = xMouse >= windowRect.Left && xMouse <= windowRect.Right && yMouse >= windowRect.Top &&
                         yMouse <= windowRect.Bottom;

        // 过滤不在窗口内的请求
        if (!isInWindow)
            return nint.Zero;

        // 如果 CanResize 为 False，直接返回 HTCLIENT
        if (!canResize)
            return new nint(HTCLIENT);

        // 真实像素尺寸的 offset
        var dpi = VisualTreeHelper.GetDpi(this);
        var offsetPxX = offsetWpf * dpi.DpiScaleX;
        var offsetPxY = offsetWpf * dpi.DpiScaleY;
        var hitWidthPxX = hitWidthWpf * dpi.DpiScaleX;
        var hitWidthPxY = hitWidthWpf * dpi.DpiScaleY;

        // 计算鼠标相对于窗口左上角的物理像素位置
        var relX = xMouse - windowRect.Left;
        var relY = yMouse - windowRect.Top;
        var w = windowBounds.Width;
        var h = windowBounds.Height;

        // 判定是否命中偏移后的热区
        var inLeft = relX >= offsetPxX && relX <= offsetPxX + hitWidthPxX             && (relY >= offsetPxY && relY <= h - offsetPxY);
        var inRight = relX <= w - offsetPxX && relX >= w - offsetPxX - hitWidthPxX    && (relY >= offsetPxY && relY <= h - offsetPxY);
        var inTop = relY >= offsetPxY && relY <= offsetPxY + hitWidthPxY              && (relX >= offsetPxX && relX <= w - offsetPxX);
        var inBottom = relY <= h - offsetPxY && relY >= h - offsetPxY - hitWidthPxY   && (relX >= offsetPxX && relX <= w - offsetPxX);

        handled = true; // 接管该区域的消息

        // 返回结果
        if (inTop && inLeft)
            return new nint(HTTOPLEFT);
        if (inTop && inRight)
            return new nint(HTTOPRIGHT);
        if (inBottom && inLeft)
            return new nint(HTBOTTOMLEFT);
        if (inBottom && inRight)
            return new nint(HTBOTTOMRIGHT);
        if (inLeft)
            return new nint(HTLEFT);
        if (inRight)
            return new nint(HTRIGHT);
        if (inTop)
            return new nint(HTTOP);
        if (inBottom)
            return new nint(HTBOTTOM);

        // 如果在 0-offset 范围内，返回 HTCLIENT 杀掉默认缩放
        return new nint(HTCLIENT);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        // 硬件加速
        if (Config.System.DisableHardwareAcceleration)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource is not null) hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
        }

        base.OnSourceInitialized(e);

        // 获取当前窗口句柄
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        if (source is not null)
        {
            // 渲染层允许 Alpha 通道通过
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            // 魔改窗口边缘判定
            source.AddHook(_SizeWndProc);
        }

        // 设置 DWM 窗口框架
        try
        {
            WindowInterop.ExtendFrameIntoClientArea(hwnd, -1);
        }
        catch (Exception ex)
        {
            LogWrapper.Error("DWM 窗口框架应用失败: " + ex.Message);
        }
    }

    // 关闭
    private void FormMain_Closing(object sender, CancelEventArgs e)
    {
        EndProgram(true);
        e.Cancel = true;
    }

    /// <summary>
    ///     正常关闭程序。程序将在执行此方法后约 0.3s 退出。
    /// </summary>
    /// <param name="sendWarning">是否在还有下载任务未完成时发出警告。</param>
    /// <param name="isUpdating">是否正在更新重启</param>
    public void EndProgram(bool sendWarning, bool isUpdating = false)
    {
        // 发出警告
        if (sendWarning && NetFacade.HasDownloadingTask())
        {
            if ((DialogService.Instance.Show(new TextDialogRequest
            {
                Title = Lang.Text("Common.Dialog.Title"),
                Content = Lang.Text("Main.Exit.HasDownloadingTask"),
                Buttons = DialogButtons.OkCancel,
                ForceWait = true
            }).Result?.Button ?? 0) == 1)
                // 强行结束下载任务
                Common.RunInNewThread(() =>
                {
                    Common.Log("[System] 正在强行停止任务");
                    foreach (var Task in Loading.loaderTaskbar.ToList())
                        Task.Abort();
                }, "强行停止下载任务");
            else
                return;
        }

        // 关闭联机大厅
        // Await LobbyController.CloseAsync().ConfigureAwait(False)
        // 关闭
        Common.RunInUiWait(() =>
        {
            // 清理视频背景
            VideoBack.Stop();
            VideoBack.Source = null;
            VideoBack.Close();
            IsHitTestVisible = false;
            if (RenderTransform is null)
            {
                var transformPos = new TranslateTransform(0d, 0d);
                var transformRotate = new RotateTransform(0d);
                var transformScale = new ScaleTransform(1d, 1d);
                transformScale.CenterX = Width / 2d;
                transformScale.CenterY = Height / 2d;
                RenderTransform = new TransformGroup
                    { Children = new TransformCollection([transformRotate, transformPos, transformScale]) };
                Animations.AniStart(new[]
                {
                    Animations.AaOpacity(this, -Opacity, 140, 40,
                        new Animations.AniEaseOutFluent(Animations.AniEasePower.Weak)),
                    Animations.AaDouble(i =>
                    {
                        transformScale.ScaleX += (double)i;
                        transformScale.ScaleY += (double)i;
                    }, 0.88d - transformScale.ScaleX, 180),
                    Animations.AaDouble(i => transformPos.Y += (double)i,
                        20d - transformPos.Y, 180, 0,
                        new Animations.AniEaseOutFluent(Animations.AniEasePower.Weak)),
                    Animations.AaDouble(i => transformRotate.Angle += (double)i,
                        0.6d - transformRotate.Angle, 180, 0,
                        new Animations.AniEaseInoutFluent(Animations.AniEasePower.Weak)),
                    Animations.AaCode(() =>
                    {
                        IsHitTestVisible = false;
                        Visibility = Visibility.Collapsed;
                        ShowInTaskbar = false;
                    }, 210),
                    Animations.AaCode(() => EndProgramForce(force: false, isUpdating: isUpdating), 230)
                }, "Form Close");
            }
            else
            {
                EndProgramForce(force: false, isUpdating: isUpdating);
            }

            Common.Log("[System] 收到关闭指令");
        });
    }

    private static bool isLogShown;

    public static void EndProgramForce(Common.ProcessReturnValues returnCode = Common.ProcessReturnValues.Success,
        bool force = true, bool isUpdating = false)
    {
        // On Error Resume Next
        // 关闭联机大厅
        // Await LobbyController.CloseAsync().ConfigureAwait(False)
        Common.isProgramEnded = true;
        Animations.AniBeginControl();
        if (UpdateManager.isUpdateWaitingRestart && !isUpdating)
            UpdateManager.UpdateRestart(false, false);
        if (returnCode == Common.ProcessReturnValues.Exception)
        {
            if (!isLogShown)
            {
                Common.FeedbackInfo();
                Common.Log("请在 https://github.com/PCL-Community/PCL2-CE/issues 提交错误报告，以便于社区解决此问题！（这也有可能是原版 PCL 的问题）");
                isLogShown = true;
                Common.ShellOnly(LogWrapper.CurrentLogger.CurrentLogFiles.Last());
            }

            Thread.Sleep(500); // 防止 PCL 在记事本打开前就被掐掉
        }

        Common.Log("[System] 程序已退出，返回值：" + Common.GetStringFromEnum(returnCode));
        // If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
        // Process.GetCurrentProcess.Kill()
        Lifecycle.Shutdown((int)returnCode, force);
    }

    private void BtnTitleClose_Click(object sender, EventArgs e)
    {
        EndProgram(true);
    }

    // 移动
    private void FormDragMove(object sender, MouseButtonEventArgs e)
    {
        // On Error Resume Next
        if (((Grid)sender).IsMouseDirectlyOver)
            DragMove();
    }

    // 改变大小
    /// <summary>
    ///     是否可以向注册表储存尺寸改变信息。以此避免初始化时误储存。
    /// </summary>
    public bool isSizeSaveable;

    private void FormMain_SizeChanged(object? sender = null, EventArgs? e = null)
    {
        if (isSizeSaveable)
        {
            States.UI.WindowHeight = Height;
            States.UI.WindowWidth = Width;
        }

        if (PanBack is not null)
        {
            RectForm.Rect = new Rect(0d, 0d, PanBack.ActualWidth, PanBack.ActualHeight);

            var formWidth = PanBack.ActualWidth + 0.001d;
            var formHeight = PanBack.ActualHeight + 0.001d;

            PanForm.Width = formWidth;
            PanForm.Height = formHeight;
            PanMain.Width = formWidth;

            if (PanTitle is not null)
                PanMain.Height = Math.Max(0d, formHeight - PanTitle.ActualHeight);
            else
                PanMain.Height = formHeight;

            VideoBack.Width = formWidth;
            VideoBack.Height = formHeight;
        }

        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal; // 修复 #1938
    }

    // 标题栏改变大小
    private void PanTitle_SizeChanged(object sender, EventArgs e)
    {
        if (PanTitleMain.ColumnDefinitions[0].ActualWidth - 30 <= 0)
            PanTitleLeft.ColumnDefinitions[0].MaxWidth = 0;
        else
            PanTitleLeft.ColumnDefinitions[0].MaxWidth = PanTitleMain.ColumnDefinitions[0].ActualWidth - 30;
    }

    // 最小化
    private void BtnTitleMin_Click(object sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnTitleHelp_Click(object sender, EventArgs e)
    {
        Common.OpenWebsite("https://www.bilibili.com/video/BV1uT4y1P7CX");
    }

    #endregion

    #region 窗体事件

    public void AddResizer()
    {
        canResize = true;
    }

    public void RemoveResizer()
    {
        canResize = false;
    }

    // 按键事件
    private void FormMain_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat)
            return;
        // 调用弹窗：回车选择第一个，Esc 选择最后一个
        if (PanMsg.Children.Count > 0)
        {
            if (e.Key == Key.Enter)
            {
                var msg = PanMsg.Children[0];
                Action? enterAction = msg switch
                {
                    MyMsgInput input => () => input.Btn1_Click(sender, null),
                    MyMsgSelect select => () => select.Btn1_Click(sender, null),
                    MyMsgText text => () => text.Btn1_Click(sender, null),
                    MyMsgMarkdown markdown => () => markdown.Btn1_Click(sender, null),
                    _ => null
                };
                enterAction?.Invoke();
                return;
            }

            if (e.Key == Key.Escape)
            {
                var msg = PanMsg.Children[0];
                Action? escapeAction = msg switch
                {
                    MyMsgInput input => input.Btn2.Visibility == Visibility.Visible
                        ? () => input.Btn2_Click(sender, null)
                        : () => input.Btn1_Click(sender, null),
                    MyMsgSelect select => select.Btn2.Visibility == Visibility.Visible
                        ? () => select.Btn2_Click(sender, null)
                        : () => select.Btn1_Click(sender, null),
                    MyMsgText text => text.Btn3.Visibility == Visibility.Visible
                        ? () => text.Btn3_Click(sender, null)
                        : text.Btn2.Visibility == Visibility.Visible
                            ? () => text.Btn2_Click(sender, null)
                            : () => text.Btn1_Click(sender, null),
                    MyMsgMarkdown markdown => markdown.Btn3.Visibility == Visibility.Visible
                        ? () => markdown.Btn3_Click(sender, null)
                        : markdown.Btn2.Visibility == Visibility.Visible
                            ? () => markdown.Btn2_Click(sender, null)
                            : () => markdown.Btn1_Click(sender, null),
                    _ => null
                };
                escapeAction?.Invoke();
                return;
            }
        }

        // 按 ESC 返回上一级
        if (e.Key == Key.Escape)
            TriggerPageBack();
        // KSP: F12 隐藏功能切换已随 MC 设置页剥离。

        // 按 F5 刷新页面
        if (e.Key == Key.F5)
        {
            if (pageLeft is IRefreshable)
                ((IRefreshable)pageLeft).Refresh();
            if (pageRight is IRefreshable)
                ((IRefreshable)pageRight).Refresh();
            return;
        }

        // 修复按下 Alt 后误认为弹出系统菜单导致的冻结
        if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
            e.Handled = true;
    }

    private void FormMain_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // 鼠标侧键返回上一级
        if (Launcher.frmMain!.PanMsg.Children.Count > 0)
            return; // 弹窗中（#5513）
        if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2)
            TriggerPageBack();
    }

    private void TriggerPageBack()
    {
        // KSP: 原 MC 下载/实例安装页返回拦截已剥离。
        PageBack();
    }

    // 切回窗口
    private void FormMain_Activated(object sender, EventArgs e)
    {
        // KSP: 原 MC 剪贴板/Mod 自动刷新已剥离。
    }

    private IDataObject _HandleDrag_PrevData;
    private DragDropEffects _HandleDrag_PrevEffects;

    // 文件拖放
    private void HandleDrag(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Handled && e.Effects != DragDropEffects.None)
                return;
            // 缓存
            e.Handled = true;
            if (ReferenceEquals(e.Data, _HandleDrag_PrevData))
            {
                e.Effects = _HandleDrag_PrevEffects;
                return;
            }

            // 确定拖放效果
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var str = (string)e.Data.GetData(DataFormats.Text);
                // KSP: 原 authlib-injector 拖拽已剥离。
                if (str.StartsWithF("file:///")) e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files is not null && files.Length > 0) e.Effects = DragDropEffects.Link;
            }

            _HandleDrag_PrevData = e.Data;
            _HandleDrag_PrevEffects = e.Effects;
            Common.Log("[System] 设置拖放类型：" + Common.GetStringFromEnum(e.Effects));
        }
        catch (Exception ex)
        {
            Common.Log(
                ex,
                "处理拖放时出错",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    private void FrmMain_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                // 获取文本
                try
                {
                    var str = (string)e.Data.GetData(DataFormats.Text);
                    Common.Log("[System] 接受文本拖拽：" + str);
                    // KSP: 原 authlib-injector 拖拽已剥离。
                    if (str.StartsWithF("file:///"))
                    {
                        // 文件拖拽（例如从浏览器下载窗口拖入）
                        var filePath = WebUtility.UrlDecode(str).Substring("file:///".Length).Replace("/", @"\");
                        e.Handled = true;
                        e.Effects = DragDropEffects.Copy;
                        FileDrag(new List<string> { filePath });
                    }
                }
                catch (Exception ex)
                {
                    Common.Log(ex, "无法接取文本拖拽事件", Common.LogLevel.Developer);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取文件并检查
                var filePathRaw = e.Data.GetData(DataFormats.FileDrop);
                if (filePathRaw is null) // #2690
                {
                    HintService.Show(Hints.Error(Lang.Text("Main.FileDrag.ExtractFirst")));
                    return;
                }

                e.Handled = true;
                e.Effects = DragDropEffects.Link;
                FileDrag((IEnumerable<string>)filePathRaw);
            }
        }
        catch (Exception ex)
        {
            Common.Log(
                ex,
                "接取拖拽事件失败",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    private void FileDrag(IEnumerable<string> filePathList)
    {
        // KSP: 原 MC 文件拖拽关联（Mod/资源包/光影/存档/Jar）已剥离。
    }

    // 接受到 Windows 窗体事件
    public bool isSystemTimeChanged;

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 30)
        {
            var nowDate = DateTime.Now;
            if (nowDate.Date == Common.applicationOpenTime.Date)
            {
                Common.Log("[System] 系统时间微调为：" + nowDate.ToLongDateString() + " " + nowDate.ToLongTimeString());
                isSystemTimeChanged = false;
            }
            else
            {
                Common.Log("[System] 系统时间修改为：" + nowDate.ToLongDateString() + " " + nowDate.ToLongTimeString());
                isSystemTimeChanged = true;
            }
        }
        else if (msg == 400 * 16 + 2)
        {
            Common.Log("[System] 收到置顶信息：" + hwnd.ToInt64());
            if (!isWindowLoadFinished)
            {
                Common.Log("[System] 窗口尚未加载完成，忽略置顶请求");
                return nint.Zero;
            }

            ShowWindowToTop();
            handled = true;
        }
        else if (msg == 26) // WM_SETTINGCHANGE
        {
            if (Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
            {
                Common.Log($"[System] 系统主题更改，深色模式：{SystemTheme.IsSystemInDarkMode()}");
                if (Config.Preference.Theme.ColorMode == ColorMode.System &&
                    (ThemeManager.IsDarkMode != SystemTheme.IsSystemInDarkMode())) ThemeService.RefreshColorMode();
            }
        }

        return nint.Zero;
    }

    // 窗口隐藏与置顶
    public bool Hidden
    {
        get => field;
        set
        {
            if (field == value)
                return;
            field = value;
            if (value)
            {
                // 隐藏
                Left -= 10000d;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                Common.Log("[System] 窗口已隐藏，位置：(" + Left + "," + Top + ")");
            }
            else
            {
                // 取消隐藏
                if (Left < -2000)
                    Left += 10000d;
                ShowWindowToTop();
            }
        }
    }

    // 解决龙猫的非通用实现史山
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (Hidden)
            Hidden = false;
    }

    /// <summary>
    ///     把当前窗口拖到最前面。
    /// </summary>
    public void ShowWindowToTop()
    {
        Common.RunInUi(() =>
        {
            // 这一坨乱七八糟的，别改，改了指不定就炸了，自己电脑还复现不出来
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
            Hidden = false;
            Topmost = true; // 偶尔 SetForegroundWindow 失效
            Topmost = false;
            Launcher.SetForegroundWindow();
            Focus();
            Common.Log($"[System] 窗口已置顶，位置：({Left}, {Top}), {Width} x {Height}");
        });
    }

    // 背景视频循环播放
    private void VideoEnded(object sender, RoutedEventArgs e)
    {
        VideoBack.Position = TimeSpan.Zero;
        VideoBack.Play();
    }

    // 最小化时暂停背景视频
    private void WindowStateChanged(object sender, EventArgs e)
    {
        // KSP: 原背景视频（VideoBackground）最小化暂停已剥离。
    }

    #endregion

    #region 切换页面

    // 页面种类与属性
    // 注意，这一枚举在 “切换页面” EventType 中调用，应视作公开 API 的一部分
    /// <summary>
    ///     页面种类。
    /// </summary>
    public enum PageType
    {
        /// <summary>
        ///     启动。
        /// </summary>
        Launch = 0,

        /// <summary>
        ///     下载。
        /// </summary>
        Download = 1,

        /// <summary>
        ///     联机。
        /// </summary>
        Tools = 3,

        /// <summary>
        ///     设置。
        /// </summary>
        Setup = 2,

        /// <summary>
        ///     实例选择。这是一个副页面。
        /// </summary>
        InstanceSelect = 5,

        /// <summary>
        ///     任务管理。这是一个副页面。
        /// </summary>
        TaskManager = 6,

        /// <summary>
        ///     实例设置。这是一个副页面。
        /// </summary>
        InstanceSetup = 7,

        /// <summary>
        ///     资源工程详情。这是一个副页面。
        /// </summary>
        CompDetail = 8,

        /// <summary>
        ///     帮助详情。这是一个副页面。
        /// </summary>
        HelpDetail = 9,

        /// <summary>
        ///     游戏实时日志。这是一个副页面。
        /// </summary>
        GameLog = 10,

        /// <summary>
        ///     存档详细管理，这是一个副页面。
        /// </summary>
        VersionSaves = 12,
    }

    /// <summary>
    ///     次要页面种类。其数值必须与 StackPanel 中的下标一致。
    /// </summary>
    public enum PageSubType
    {
        Default = 0,
        DownloadInstall = 1,
        DownloadMod = 2,
        DownloadPack = 3,
        DownloadDataPack = 4,
        DownloadResourcePack = 5,
        DownloadShader = 6,
        DownloadWorld = 7,
        DownloadCompFavorites = 8,
        DownloadClient = 9,
        DownloadOptiFine = 10,
        DownloadForge = 11,
        DownloadNeoForge = 12,
        DownloadCleanroom = 13,
        DownloadFabric = 14,
        DownloadLiteLoader = 16,
        DownloadLabyMod = 17,
        DownloadLegacyFabric = 18,

        SetupLaunch = 0,
        SetupUI = 1,
        SetupGameManage = 2,
        SetupLink = 3,
        SetupAbout = 4,
        SetupLog = 5,
        SetupFeedback = 6,
        SetupGameLink = 7,
        SetupUpdate = 8,
        SetupJava = 9,
        SetupLauncherMisc = 10,
        SetupLauncherLanguage = 11,

        ToolsGameLink = 1,
        ToolsTest = 3,

        VersionOverall = 0,
        VersionSetup = 1,
        VersionExport = 2,
        VersionWorld = 3,
        VersionScreenshot = 4,
        VersionMod = 5,
        VersionModDisabled = 6,
        VersionResourcePack = 7,
        VersionShader = 8,
        VersionSchematic = 9,
        VersionInstall = 10,
        VersionServer = 11,
        VersionModJarInJar = 12,
        VersionSavesInfo = 0,
        VersionSavesDatapack = 1
    }

    /// <summary>
    ///     获取次级页面的名称。若并非次级页面则返回空字符串，故可以以此判断是否为次级页面。
    /// </summary>
    private string PageNameGet(PageStackData stack)
    {
        switch (stack.page)
        {
            case PageType.InstanceSelect:
            {
                return Lang.Text("Main.Title.InstanceSelect");
            }
            case PageType.TaskManager:
            {
                return Lang.Text("Main.Title.TaskManager");
            }
            case PageType.GameLog:
            {
                return Lang.Text("Main.Title.GameLog");
            }
            case PageType.CompDetail:
            {
                return "模组详情";
            }
            // KSP: MC 子页面标题已剥离。
            default:
            {
                return "";
            }
        }
    }

    /// <summary>
    ///     刷新次级页面的名称。
    /// </summary>
    public void PageNameRefresh(PageStackData type)
    {
        LabTitleInner.Text = PageNameGet(type);
    }

    /// <summary>
    ///     刷新次级页面的名称。
    /// </summary>
    public void PageNameRefresh()
    {
        PageNameRefresh(pageCurrent);
    }

    // 页面状态存储
    /// <summary>
    ///     当前的主页面。
    /// </summary>
    public PageStackData pageCurrent = PageType.Launch;

    /// <summary>
    ///     上一个主页面。
    /// </summary>
    public PageStackData pageLast = PageType.Launch;

    /// <summary>
    ///     当前的子页面。
    /// </summary>
    public PageSubType PageCurrentSub
    {
        get
        {
            switch (pageCurrent.page)
            {
                // KSP: MC 子页面切换已剥离。
                default:
                {
                    return 0; // 没有子页面
                }
            }
        }
    }

    /// <summary>
    ///     上层页面的编号堆栈，用于返回。
    /// </summary>
    public List<PageStackData> pageStack = new();

    public class PageStackData
    {
        /// <summary>
        /// <list type="bullet">
        ///   <item><description>CompDetail: (CompProject, ExpandedTitles, TargetVersion, TargetLoader, ResourceType)</description></item>
        ///   <item><description>VersionSaves: SavePath</description></item>
        /// </list>
        /// </summary>
        public (
            Comp.CompProject CompProject,
            List<string> ExpandedTitles,
            string TargetVersion,
            Comp.CompLoaderType TargetLoader,
            Comp.CompType ResourceType,
            string SavePath
        )? additional;

        /// <summary>
        ///     帮助详情页实例。仅在 <see cref="PageType.HelpDetail"/> 中使用。
        /// </summary>
        public object? helpPage;

        public PageType page;

        public override bool Equals(object other)
        {
            if (other is null)
                return false;
            if (other is PageStackData)
            {
                var pageOther = (PageStackData)other;
                if (page != pageOther.page)
                    return false;
                if (helpPage is not null || pageOther.helpPage is not null)
                    return ReferenceEquals(helpPage, pageOther.helpPage);
                if (additional is null) return pageOther.additional is null;

                return pageOther.additional is not null && additional.Equals(pageOther.additional);
            }

            if (other is int o)
            {
                if ((int)page == o)
                    return false;
                return additional is null;
            }

            return false;
        }

        public static bool operator ==(PageStackData left, PageStackData right)
        {
            return EqualityComparer<PageStackData>.Default.Equals(left, right);
        }

        public static bool operator !=(PageStackData left, PageStackData right)
        {
            return !(left == right);
        }

        public static implicit operator PageStackData(PageType value)
        {
            return new PageStackData { page = value };
        }

        public static implicit operator PageType(PageStackData value)
        {
            return value.page;
        }
    }

    public MyPageLeft pageLeft;
    public MyPageRight pageRight;

    public INavigationService Navigation { get; }

    private object? _pendingParameter;

    // 引发实际页面切换的入口
    private bool isChangingPage;

    /// <summary>
    ///     切换页面，并引起对应选择 UI 的改变。
    /// </summary>
    public void PageChange(PageStackData stack, PageSubType subType = PageSubType.Default, object? parameter = null)
    {
        if (string.IsNullOrEmpty(PageNameGet(stack)))
        {
            // 切换到主页面
            PageChangeExit();
            isChangingPage = true; // 防止下面的勾选直接触发了 PageChangeActual
            ((MyRadioButton)PanTitleSelect.Children[(int)stack.page]).SetChecked(true, true,
                string.IsNullOrEmpty(PageNameGet(pageCurrent)));
            isChangingPage = false;
            // KSP: 原 MC 左页勾选（下载/设置）已剥离。
            PageChangeActual(stack, subType, parameter);
        }
        else
        {
            // 切换到次页面
            // KSP: 原 MC 子页面左页勾选（实例/存档）已剥离。

            PageChangeActual(stack, subType, parameter);
        }
    }

    /// <summary>
    ///     通过点击导航栏改变页面。
    /// </summary>
    private void BtnTitleSelect_Click(MyRadioButton sender, bool raiseByMouse)
    {
        if (isChangingPage) return;
        var pageType = (PageType)int.Parse(sender.Tag.ToString());
        PageChangeActual(pageType, PageSubType.Default);
    }

    public void SelectNavTag(string tag)
    {
        var btn = tag switch
        {
            "1" => BtnTitleSelect1,
            "2" => BtnTitleSelect2,
            _ => BtnTitleSelect0,
        };
        if (btn is not null) btn.Checked = true;
    }

    private void BtnTitleInner_Click(object sender, EventArgs e)
    {
        PageBack();
    }

    /// <summary>
    ///     通过点击返回按钮或手动触发返回来改变页面。
    /// </summary>
    public void PageBack()
    {
        if (pageStack.Any())
            PageChangeActual(pageStack[0], PageSubType.Default);
        else
            PageChange(PageType.Launch);
    }

    // 实际处理页面切换
    /// <summary>
    ///     切换现有页面的实际方法。
    /// </summary>
    private void PageChangeActual(PageStackData stack, PageSubType subType, object? parameter = null)
    {
        _pendingParameter = parameter;
        if (pageCurrent == stack && (PageCurrentSub == subType || (int)subType == -1))
            return;
        Animations.AniBeginControl();
        try
        {
            #region 子页面处理

            var pageName = PageNameGet(stack);
            if (string.IsNullOrEmpty(pageName))
            {
                // 即将切换到一个顶级页面
                PageChangeExit();
            }
            // 即将切换到一个子页面
            else if (pageStack.Any())
            {
                // 子页面 → 另一个子页面，更新
                Animations.AniStart(
                    new[]
                    {
                    Animations.AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                    Animations.AaCode(() => LabTitleInner.Text = pageName, after: true),
                    Animations.AaOpacity(LabTitleInner, 1d, 150, 30)
                    }, "FrmMain Titlebar SubLayer");
                if (pageStack.Contains(stack))
                    // 返回到更上层的子页面
                    while (pageStack.Contains(stack))
                        pageStack.RemoveAt(0);
                else
                    // 进入更深层的子页面
                    pageStack.Insert(0, pageCurrent);
            }
            else
            {
                // 主页面 → 子页面，进入
                PanTitleInner.Visibility = Visibility.Visible;
                PanTitleMain.IsHitTestVisible = false;
                PanTitleInner.IsHitTestVisible = true;
                PageNameRefresh(stack);
                Animations.AniStart(
                    new[]
                    {
                    Animations.AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                    Animations.AaX(PanTitleMain, 12d - PanTitleMain.Margin.Left, 150,
                        ease: new Animations.AniEaseInFluent(Animations.AniEasePower.Weak)),
                    Animations.AaOpacity(PanTitleInner, 1d - PanTitleInner.Opacity, 150, 200),
                    Animations.AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200,
                        new Animations.AniEaseOutBack()),
                    Animations.AaCode(() => PanTitleMain.Visibility = Visibility.Collapsed, after: true)
                    }, "FrmMain Titlebar FirstLayer");
                pageStack.Insert(0, pageCurrent);
            }

            #endregion

            #region 实际更改页面框架 UI

            pageLast = pageCurrent;
            pageCurrent = stack;
            switch (stack.page)
            {
                // 已随对应页面删除。新的 KSP 导航将由独立的主页/模组管理/设置页面替代。
                case PageType.Launch: // 启动页
                    PageChangeAnim(new PageLaunchLeft(), new Pages.PageHome());
                    break;
                case PageType.Download: // 下载/模组页
                    PageChangeAnim(new PageDownloadLeft(), new PageDownloadMod());
                    break;
                case PageType.Setup: // 设置页
                    {
                        if (settingsLeft is null)
                        {
                            settingsLeft = new PageSettingsLeft();
                            settingsLeft.NavChanged += (key) =>
                            {
                                settingsNav = key;
                                FrameworkElement right = key switch
                                {
                                    "ksp" => new Pages.PageKspInstance(),
                                    "ckan" => new Pages.PageCkan(),
                                    "translate" => new Pages.PageTranslate(),
                                    _ => new Pages.PageAbout(),
                                };
                                if (PanMainRight.Child is MyPageRight oldRight) oldRight.PageOnExit();
                                PanMainRight.Child = right;
                                if (right is MyPageRight newRight) newRight.PageOnEnter();
                            };
                        }
                        FrameworkElement right = settingsNav switch
                        {
                            "ksp" => new Pages.PageKspInstance(),
                            "ckan" => new Pages.PageCkan(),
                            "translate" => new Pages.PageTranslate(),
                            _ => new Pages.PageAbout(),
                        };
                        PageChangeAnim(settingsLeft, right);
                    }
                    break;
                case PageType.CompDetail: // 模组详情
                {
                    Launcher.frmDownloadCompDetail ??= new PageModDetail();
                    PageChangeAnim(new MyPageLeft(), Launcher.frmDownloadCompDetail);
                    break;
                }
                default: break;
            }

            #endregion

            #region 设置为最新状态

            BtnExtraDownload.ShowRefresh();
            BtnExtraApril.ShowRefresh();

            #endregion

            Common.Log("[Control] 切换主要页面：" + Common.GetStringFromEnum(stack) + ", " + (int)subType);
        }
        catch (Exception ex)
        {
            Common.Log(
                ex,
                "切换主要页面失败（ID " + (int)pageCurrent.page + "）",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
        finally
        {
            Animations.AniEndControl();
        }
    }

    private void PageChangeAnim(FrameworkElement targetLeft, FrameworkElement targetRight)
    {
        Animations.AniStop("FrmMain LeftChange");
        Animations.AniStop("PageLeft PageChange"); // 停止左边栏变更导致的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        Animations.AniBeginControl();
        try
        {
            // 清除新页面关联性
            if (targetLeft.Parent is not null)
                targetLeft.SetValue(ContentPresenter.ContentProperty, null);
            if (targetRight is not null && targetRight.Parent is not null)
                targetRight.SetValue(ContentPresenter.ContentProperty, null);
            pageLeft = (MyPageLeft)targetLeft;
            pageRight = (MyPageRight)targetRight;
            // 触发页面通用动画
            (PanMainLeft.Child as IPageView)?.OnNavigatedFrom();
            ((MyPageLeft)PanMainLeft.Child).TriggerHideAnimation();
            (PanMainRight.Child as IPageView)?.OnNavigatedFrom();
            ((MyPageRight)PanMainRight.Child).PageOnExit();
        }
        finally
        {
            Animations.AniEndControl();
        }
        // 执行动画
        Animations.AniStart(new[]
        {
            Animations.AaCode(() =>
            {
                Animations.AniBeginControl();
                try
                {
                    // 把新页面添加进容器
                    PanMainLeft.Child = pageLeft;
                    pageLeft.Opacity = 0d;
                    PanMainLeft.Background = null;
                }
                finally
                {
                    Animations.AniEndControl();
                }
                Common.RunInUi(() => PanMainLeft_Resize(PanMainLeft.ActualWidth), true);
            }, 110),
            Animations.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                pageLeft.Opacity = 1d;
                pageLeft.TriggerShowAnimation();
                (pageLeft as IPageView)?.OnNavigatedTo(_pendingParameter);
            }, 30, true)
        }, "FrmMain PageChangeLeft");
        Animations.AniStart(new[]
        {
            Animations.AaCode(() =>
            {
                Animations.AniBeginControl();
                try
                {
                    ((MyPageRight)PanMainRight.Child).PageOnForceExit();
                    // 把新页面添加进容器
                    PanMainRight.Child = pageRight;
                    pageRight.Opacity = 0d;
                    PanMainRight.Background = null;
                }
                finally
                {
                    Animations.AniEndControl();
                }
                Common.RunInUi(() => BtnExtraBack.ShowRefresh(), true);
            }, 110),
            Animations.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                pageRight.Opacity = 1d;
                pageRight.PageOnEnter();
                (pageRight as IPageView)?.OnNavigatedTo(_pendingParameter);
            }, 30, true)
        }, "FrmMain PageChangeRight");
        ((NavigationService)Navigation).RaiseNavigated(new RouteKey(pageCurrent.page, PageCurrentSub));
    }

    /// <summary>
    ///     退出子界面。
    /// </summary>
    private void PageChangeExit()
    {
        if (pageStack.Any())
        {
            // 子页面 → 主页面，退出
            PanTitleMain.Visibility = Visibility.Visible;
            PanTitleMain.IsHitTestVisible = true;
            PanTitleInner.IsHitTestVisible = false;
            Animations.AniStart(
                new[]
                {
                    Animations.AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                    Animations.AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,
                        ease: new Animations.AniEaseInFluent()),
                    Animations.AaOpacity(PanTitleMain, 1d - PanTitleMain.Opacity, 150, 200),
                    Animations.AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200,
                        new Animations.AniEaseOutBack(Animations.AniEasePower.Weak)),
                    Animations.AaCode(() => PanTitleInner.Visibility = Visibility.Collapsed, after: true)
                }, "FrmMain Titlebar FirstLayer");
            pageStack.Clear();
        }
        // 主页面 → 主页面，无事发生
    }

    // 左边栏改变
    private void PanMainLeft_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;
        PanMainLeft_Resize(e.NewSize.Width);
    }

    private void PanMainLeft_Resize(double newWidth)
    {
        var delta = newWidth - RectLeftBackground.Width;
        if (Math.Abs(delta) > 0.1d && !Animations.AniExitsAnyControl())
        {
            if (PanMain.Opacity < 0.1d)
                PanMainLeft.IsHitTestVisible = false; // 避免左边栏指向背景未能完美覆盖左边栏
            if (newWidth > 0d)
                // 宽度足够，显示
                Animations.AniStart(
                    new[]
                    {
                        Animations.AaWidth(RectLeftBackground, newWidth - RectLeftBackground.Width, 180,
                            ease: new Animations.AniEaseOutFluent(Animations.AniEasePower.ExtraStrong)),
                        Animations.AaOpacity(RectLeftShadow, 1d - RectLeftShadow.Opacity, 180),
                        Animations.AaCode(() => PanMainLeft.IsHitTestVisible = true, 150)
                    }, "FrmMain LeftChange", true);
            else
                // 宽度不足，隐藏
                Animations.AniStart(
                    new[]
                    {
                        Animations.AaWidth(RectLeftBackground, -RectLeftBackground.Width, 180,
                            ease: new Animations.AniEaseOutFluent()),
                        Animations.AaOpacity(RectLeftShadow, -RectLeftShadow.Opacity, 180),
                        Animations.AaCode(() => PanMainLeft.IsHitTestVisible = true, 150)
                    }, "FrmMain LeftChange", true);
        }
        else
        {
            RectLeftBackground.Width = newWidth;
            PanMainLeft.IsHitTestVisible = true;
            Animations.AniStop("FrmMain LeftChange");
        }
    }

    #endregion

    #region 控件拖动

    // 在时钟中调用，使得即使鼠标在窗口外松开，也可以释放控件
    public void DragTick()
    {
        if (Launcher.dragControl is null)
            return;
        if (!(Mouse.LeftButton == MouseButtonState.Pressed)) DragStop();
    }

    // 在鼠标移动时调用，以改变 Slider 位置
    public void DragDoing()
    {
        if (Launcher.dragControl is null)
            return;
        if (Mouse.LeftButton == MouseButtonState.Pressed)
        {
            Launcher.dragControl.DragDoing();
        }
        else
            DragStop();
    }

    private void PanBack_MouseMove(object sender, EventArgs e)
    {
        DragDoing();
    }

    public void DragStop()
    {
        // 存在其他线程调用的可能性，因此需要确保在 UI 线程运行
        Common.RunInUi(() =>
        {
            if (Launcher.dragControl is null)
                return;
            var control = Launcher.dragControl;
            Launcher.dragControl = null;
            control.DragStop(); // 控件会在该事件中判断 DragControl，所以得放在后面
        });
    }

    #endregion

    #region 附加按钮

    // 更新重启
    private void BtnExtraUpdateRestart_Click(object sender, MouseButtonEventArgs e)
    {
        UpdateManager.UpdateRestart(true);
    }

    private bool BtnExtraUpdateRestart_ShowCheck()
    {
        return UpdateManager.isUpdateWaitingRestart;
    }

    // 音乐
    private void BtnExtraMusic_Click(object sender, MouseButtonEventArgs e)
    {
        // KSP: 音乐模块已剥离。
    }

    private void BtnExtraMusic_RightClick(object sender, MouseButtonEventArgs e)
    {
        // KSP: 音乐模块已剥离。
    }

    // 任务管理
    private void BtnExtraDownload_Click(object sender, MouseButtonEventArgs e)
    {
        PageChange(PageType.TaskManager);
    }

    private bool BtnExtraDownload_ShowCheck()
    {
        return NetFacade.HasDownloadingTask() && !(pageCurrent == PageType.TaskManager);
    }

    // 投降
    public void AprilGiveup()
    {
        if (Launcher.isAprilEnabled && !Launcher.isAprilGiveup)
        {
            HintService.Show(Hints.Success("=D"));
            Launcher.isAprilGiveup = true;
            BtnExtraApril.ShowRefresh();
        }
    }

    private void BtnExtraApril_Click(object sender, MouseButtonEventArgs e)
    {
        AprilGiveup();
    }

    public bool BtnExtraApril_ShowCheck()
    {
        return Launcher.isAprilEnabled && !Launcher.isAprilGiveup && pageCurrent == PageType.Launch;
    }

    // 关闭 Minecraft
    private void BtnExtraShutdown_Click(object sender, MouseButtonEventArgs e)
    {
        // KSP: 原强制关闭 Minecraft 已剥离。
    }

    public bool BtnExtraShutdown_ShowCheck()
    {
        return false; // KSP: 无 MC 运行
    }

    // 游戏日志
    private void BtnExtraLog_Click(object sender, MouseButtonEventArgs e)
    {
        PageChange(PageType.GameLog);
    }

    public bool BtnExtraLog_ShowCheck()
    {
        return false; // KSP: 游戏日志页已剥离
    }

    /// <summary>
    ///     返回顶部。
    /// </summary>
    public void BackToTop()
    {
        var realScroll = BtnExtraBack_GetRealChild();
        if (realScroll is not null)
            realScroll.PerformVerticalOffsetDelta(-realScroll.VerticalOffset);
        else
            Common.Log(
                "[UI] 无法返回顶部，未找到合适的 RealScroll",
                Common.LogLevel.Hint,
                userSummary: Lang.Text("Main.Error.ScrollToTopFailed"));
    }

    private void BtnExtraBack_Click(object sender, MouseButtonEventArgs e)
    {
        BackToTop();
    }

    private bool BtnExtraBack_ShowCheck()
    {
        var realScroll = BtnExtraBack_GetRealChild();
        return realScroll is not null && realScroll.Visibility == Visibility.Visible &&
               realScroll.VerticalOffset > Height + (BtnExtraBack.Show ? 0 : 700);
    }

    private MyScrollViewer? BtnExtraBack_GetRealChild()
    {
        if (PanMainRight.Child is null || !(PanMainRight.Child is MyPageRight))
            return null;
        return ((MyPageRight)PanMainRight.Child).PanScroll;
    }

    #endregion
}




















