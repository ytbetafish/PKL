using System;
using System.Windows;
using System.Windows.Controls;

namespace PCL;

/// <summary>
/// KSP 实例设置页左侧（对应原版 PageInstanceLeft.xaml.cs）。
/// 原版导航：概览 / 设置 / 安装 / 导出 / 存档 / 截图 / 模组 / 禁用模组 / 资源包 / 光影 / 投影 / 服务器；
/// KSP 首批仅保留概览，模组 / 存档 / 截图子页在后续批次接入。
/// </summary>
public partial class PageInstanceLeft : IRefreshable, IPageView
{
    private static PageInstanceOverall? _overall;
    private static PageInstanceCompResource? _mods;
    private static PageInstanceSaves? _saves;
    private static PageInstanceScreenshot? _shots;
    private bool _refreshed;

    public FormMain.PageSubType PageId = FormMain.PageSubType.VersionOverall;

    public PageInstanceLeft()
    {
        InitializeComponent();
        AnimatedControl = PanItem;
        Loaded += (_, _) => RefreshOnNavigate();
        Unloaded += (_, _) => _refreshed = false;
    }

    public void Refresh() => Refresh(Launcher.frmMain.PageCurrentSub);

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom()
    {
    }

    private void RefreshOnNavigate()
    {
        if (_refreshed)
            return;
        _refreshed = true;
    }

    public void Refresh(FormMain.PageSubType subType)
    {
        if (subType == FormMain.PageSubType.VersionOverall)
            ItemOverall.Checked = true;
        else if (subType == FormMain.PageSubType.VersionMod)
            ItemMod.Checked = true;
        else if (subType == FormMain.PageSubType.VersionWorld)
            ItemWorld.Checked = true;
        else if (subType == FormMain.PageSubType.VersionScreenshot)
            ItemScreenshot.Checked = true;
    }

    private void PageCheck(object sender, Common.RouteEventArgs e)
    {
        if (sender is MyListItem { Tag: { } tag })
            PageChange((FormMain.PageSubType)Common.Val(tag));
    }

    public object PageGet(FormMain.PageSubType id)
    {
        if ((int)id == -1)
            id = PageId;
        switch (id)
        {
            case FormMain.PageSubType.VersionOverall:
                return _overall ??= new PageInstanceOverall();
            case FormMain.PageSubType.VersionMod:
                return _mods ??= new PageInstanceCompResource();
            case FormMain.PageSubType.VersionWorld:
                return _saves ??= new PageInstanceSaves();
            case FormMain.PageSubType.VersionScreenshot:
                return _shots ??= new PageInstanceScreenshot();
            default:
                throw new Exception("未知的实例设置子页面类型: " + id);
        }
    }

    public void PageChange(FormMain.PageSubType id)
    {
        if (PageId == id)
            return;
        Animations.AniBeginControl();
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            PageId = id;
        }
        catch (Exception ex)
        {
            Common.Log(ex, "切换实例设置子页面失败（ID " + (int)id + "）", Common.LogLevel.Feedback);
        }
        finally
        {
            Animations.AniEndControl();
        }
    }

    private static void PageChangeRun(MyPageRight target)
    {
        Animations.AniStop("FrmMain PageChangeRight");
        if (target.Parent is not null)
            target.SetValue(ContentPresenter.ContentProperty, null);
        Launcher.frmMain.pageRight = target;
        ((MyPageRight)Launcher.frmMain.PanMainRight.Child).PageOnExit();
        Animations.AniStart(new[]
        {
            Animations.AaCode(() =>
            {
                ((MyPageRight)Launcher.frmMain.PanMainRight.Child).PageOnForceExit();
                Launcher.frmMain.PanMainRight.Child = Launcher.frmMain.pageRight;
                Launcher.frmMain.pageRight.Opacity = 0d;
            }, 130),
            Animations.AaCode(() =>
            {
                Launcher.frmMain.pageRight.Opacity = 1d;
                Launcher.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }
}
