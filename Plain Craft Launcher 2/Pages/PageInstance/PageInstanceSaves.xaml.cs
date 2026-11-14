using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PCL;

/// <summary>
/// KSP 实例设置页 · 存档（对应原版 PageInstanceSaves）。
/// 原版 MC 存档的 .dat 解析、数据包、存档详情子页已裁剪；KSP 存档即 saves 目录下的子文件夹。
/// 目录枚举在后台线程进行，页面先显示加载动画，完成后再回填列表，避免界面假死。
/// </summary>
public partial class PageInstanceSaves : IPageView
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private bool _refreshed;

    public PageInstanceSaves()
    {
        InitializeComponent();
        Common.Log($"[Saves] 构造完成 {_sw.ElapsedMilliseconds} ms");
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
        Common.Log($"[Saves] RefreshOnNavigate 开始 {_sw.ElapsedMilliseconds} ms");

        PanLoad.Visibility = Visibility.Visible;
        PanListCard.Visibility = Visibility.Collapsed;

        var root = PklSettings.Current.KspRootPath;
        var savesDir = string.IsNullOrWhiteSpace(root) ? null : Path.Combine(root, "saves");

        List<string> dirs;
        try
        {
            var t0 = _sw.ElapsedMilliseconds;
            dirs = await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(savesDir) || !Directory.Exists(savesDir))
                    return new List<string>();
                return Directory.GetDirectories(savesDir).OrderBy(Path.GetFileName).ToList();
            });
            Common.Log($"[Saves] 后台枚举完成 {_sw.ElapsedMilliseconds} ms（枚举段 {_sw.ElapsedMilliseconds - t0} ms），目录 {dirs.Count}");
        }
        catch (Exception ex)
        {
            dirs = new List<string>();
            Common.Log(ex, "存档列表加载失败");
        }

        PanList.Children.Clear();
        if (dirs.Count == 0)
        {
            PanList.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(savesDir) ? "未找到存档目录（saves）" : "暂无存档",
                Margin = new Thickness(15, 10, 0, 10),
                Opacity = 0.6,
                FontSize = 13
            });
        }
        else
        {
            foreach (var dir in dirs)
                PanList.Children.Add(CreateItem(dir));
        }

        PanLoad.Visibility = Visibility.Collapsed;
        PanListCard.Visibility = Visibility.Visible;
        Common.Log($"[Saves] 回填完成 {_sw.ElapsedMilliseconds} ms");
    }

    private MyListItem CreateItem(string dir)
    {
        var item = new MyListItem
        {
            Title = Path.GetFileName(dir),
            Info = dir,
            SvgIcon = "lucide/globe",
            LogoScale = 1.05d,
            tag = dir,
            Height = 42d,
            Type = MyListItem.CheckType.None
        };
        item.ContentHandler = (i, _) =>
        {
            var btn = new MyIconButton { LogoScale = 1.1d, SvgIcon = "lucide/folder-open" };
            btn.ToolTip = "打开存档目录";
            btn.Click += (_, _) => OpenFolder(dir);
            i.Buttons = new[] { btn };
        };
        return item;
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
