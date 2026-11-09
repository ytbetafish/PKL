using System.Windows.Controls;
using PCL.Core.UI.Hints;

namespace PCL;

public partial class PageDownloadLeft
{
    public static Pages.PageMods? DownloadModPage;
    public static Pages.PageMods? DownloadPackPage;
    public static Pages.PageMods? DownloadFavoritesPage;

    public FormMain.PageSubType PageId = FormMain.PageSubType.DownloadMod;

    public PageDownloadLeft()
    {
        InitializeComponent();
        AnimatedControl = PanItem;
    }

    private void PageCheck(object sender, Common.RouteEventArgs e)
    {
        if (sender is MyListItem { Tag: { } tag })
            PageChange((FormMain.PageSubType)Common.Val(tag));
    }

    public object PageGet(FormMain.PageSubType id)
    {
        if (id == default)
            id = PageId;
        switch (id)
        {
            case FormMain.PageSubType.DownloadMod:
            {
                if (DownloadModPage is null)
                    DownloadModPage = new Pages.PageMods();
                return DownloadModPage;
            }
            case FormMain.PageSubType.DownloadPack:
            {
                if (DownloadPackPage is null)
                    DownloadPackPage = new Pages.PageMods();
                return DownloadPackPage;
            }
            case FormMain.PageSubType.DownloadCompFavorites:
            {
                if (DownloadFavoritesPage is null)
                    DownloadFavoritesPage = new Pages.PageMods();
                return DownloadFavoritesPage;
            }
            default:
            {
                throw new Exception("未知的下载子页面类型: " + id);
            }
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
            Common.Log(ex, "切换下载子页面失败（ID " + (int)id + "）", Common.LogLevel.Feedback);
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

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        HintService.Show(Hints.Info("正在刷新...", log: false));
    }
}
