using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PCL;

/// <summary>
/// KSP 实例设置页 · 截图（对应原版 PageInstanceScreenshot）。
/// 列出 screenshots 目录下的图片，按修改时间倒序；Logo 直接使用本地图片路径作为缩略图。
/// 文件枚举与元数据读取在后台线程进行，页面先显示加载动画，完成后再回填列表。
/// </summary>
public partial class PageInstanceScreenshot : IPageView
{
    private bool _refreshed;

    public PageInstanceScreenshot()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshOnNavigate();
        Unloaded += (_, _) => _refreshed = false;
    }

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom()
    {
    }

    private async void RefreshOnNavigate()
    {
        if (_refreshed)
            return;
        _refreshed = true;

        PanLoad.Visibility = Visibility.Visible;
        PanListCard.Visibility = Visibility.Collapsed;

        var root = PklSettings.Current.KspRootPath;
        var shotsDir = string.IsNullOrWhiteSpace(root) ? null : Path.Combine(root, "screenshots");

        List<FileInfo> files;
        try
        {
            files = await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(shotsDir) || !Directory.Exists(shotsDir))
                    return new List<FileInfo>();
                return Directory.EnumerateFiles(shotsDir, "*.*")
                    .Where(IsImageFile)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
            });
        }
        catch (Exception ex)
        {
            files = new List<FileInfo>();
            Common.Log(ex, "截图列表加载失败", Common.LogLevel.Feedback);
        }

        PanList.Children.Clear();
        if (files.Count == 0)
        {
            PanList.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(shotsDir) ? "未找到截图目录（screenshots）" : "暂无截图",
                Margin = new Thickness(15, 10, 0, 10),
                Opacity = 0.6,
                FontSize = 13
            });
        }
        else
        {
            foreach (var file in files)
                PanList.Children.Add(CreateItem(file, shotsDir!));
        }

        PanLoad.Visibility = Visibility.Collapsed;
        PanListCard.Visibility = Visibility.Visible;
    }

    private static bool IsImageFile(string path)
    {
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private MyListItem CreateItem(FileInfo file, string shotsDir)
    {
        var item = new MyListItem
        {
            Title = file.Name,
            Info = $"{file.LastWriteTime:yyyy-MM-dd HH:mm}  ·  {FormatSize(file.Length)}",
            Logo = file.FullName,
            LogoScale = 1.0d,
            tag = file.FullName,
            Height = 46d,
            Type = MyListItem.CheckType.None
        };
        item.ContentHandler = (i, _) =>
        {
            var btn = new MyIconButton { LogoScale = 1.1d, SvgIcon = "lucide/folder-open" };
            btn.ToolTip = "打开截图目录";
            btn.Click += (_, _) => OpenFolder(shotsDir);
            i.Buttons = new[] { btn };
        };
        return item;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.0} KB";
        return $"{bytes / (1024.0 * 1024.0):0.0} MB";
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
