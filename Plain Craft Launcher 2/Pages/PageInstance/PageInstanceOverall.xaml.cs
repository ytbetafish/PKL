using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Pkl.Ckan;

namespace PCL;

/// <summary>
/// KSP 实例设置页右侧 · 概览（对应原版 PageInstanceOverall）。
/// 原版的版本图标 / 分类 / 收藏 / 重命名 / 描述 / 整合包 / 补丁核心等 MC 专属逻辑已裁剪；
/// 保留实例信息卡片（版本、目录、GameData、可执行文件、存档、截图）与目录快捷打开。
/// </summary>
public partial class PageInstanceOverall : IPageView
{
    private readonly KspInstanceService _kspService = new();
    private bool _refreshed;

    public PageInstanceOverall()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshOnNavigate();
        Unloaded += (_, _) => _refreshed = false;
    }

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom()
    {
    }

    private void RefreshOnNavigate()
    {
        if (_refreshed)
            return;
        _refreshed = true;
        BuildContent();
    }

    private void BuildContent()
    {
        PanMain.Children.Clear();
        var root = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            PanMain.Children.Add(new TextBlock
            {
                Text = "未选择有效的 KSP 实例目录",
                Margin = new Thickness(13, 18, 5, 4),
                Opacity = 0.6,
                FontSize = 13
            });
            return;
        }

        var version = ProbeVersion(root);
        var gd = Path.Combine(root, "GameData");
        var saves = Path.Combine(root, "saves");
        var screenshots = Path.Combine(root, "screenshots");
        var exe = File.Exists(Path.Combine(root, "KSP_x64.exe")) ? Path.Combine(root, "KSP_x64.exe")
                : File.Exists(Path.Combine(root, "KSP.exe")) ? Path.Combine(root, "KSP.exe")
                : "未找到 KSP_x64.exe / KSP.exe";

        var card = new MyCard
        {
            Title = "实例信息",
            Margin = new Thickness(0d, 0d, 0d, 15d)
        };
        var stack = new StackPanel
        {
            Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 0d),
            VerticalAlignment = VerticalAlignment.Top,
            Tag = "KSP 实例信息",
        };
        card.Children.Add(stack);
        card.SwapControl = stack;
        card.InstallMethod = s =>
        {
            s.Children.Add(CreateInfoItem("KSP 版本", version, "lucide/info", null));
            s.Children.Add(CreateInfoItem("实例目录", root, "lucide/server", root));
            s.Children.Add(CreateInfoItem("GameData 目录", DirInfo(gd), "lucide/folder-open",
                Directory.Exists(gd) ? gd : null));
            s.Children.Add(CreateInfoItem("可执行文件", exe, "lucide/package", null));
            s.Children.Add(CreateInfoItem("存档目录", DirInfo(saves), "lucide/folder-open",
                Directory.Exists(saves) ? saves : null));
            s.Children.Add(CreateInfoItem("截图目录", DirInfo(screenshots), "lucide/folder-open",
                Directory.Exists(screenshots) ? screenshots : null));
        };
        PanMain.Children.Add(card);
        card.StackInstall();
    }

    private MyListItem CreateInfoItem(string title, string info, string svg, string? openPath)
    {
        var item = new MyListItem
        {
            Title = title,
            Info = info,
            SvgIcon = svg,
            LogoScale = 1.05d,
            Height = 42d,
            Type = MyListItem.CheckType.None
        };
        if (openPath is not null)
        {
            item.ContentHandler = (i, _) =>
            {
                var btn = new MyIconButton { LogoScale = 1.1d, SvgIcon = "lucide/folder-open" };
                btn.ToolTip = "打开目录";
                btn.Click += (_, _) => OpenFolder(openPath);
                i.Buttons = new[] { btn };
            };
        }

        return item;
    }

    private static string DirInfo(string path)
        => Directory.Exists(path) ? path : path + "（不存在）";

    private string ProbeVersion(string root)
    {
        try
        {
            var v = _kspService.Probe(root)?.Version;
            if (!string.IsNullOrEmpty(v))
                return v;
        }
        catch
        {
            // 服务层探测失败时回退本地文件读取。
        }

        var buildId = Path.Combine(root, "KSP_Data", "buildID.txt");
        var versionTxt = Path.Combine(root, "version.txt");
        try
        {
            if (File.Exists(buildId))
                return File.ReadAllText(buildId).Trim();
            if (File.Exists(versionTxt))
                return File.ReadAllText(versionTxt).Trim();
        }
        catch
        {
            // 忽略读取失败。
        }

        return "未知";
    }

    private static void OpenFolder(string path)
    {
        if (Directory.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
    }
}
