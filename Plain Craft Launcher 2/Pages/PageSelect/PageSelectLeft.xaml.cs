using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using Pkl.Ckan;

namespace PCL;

/// <summary>
/// KSP 实例选择页左侧（对应原版 PageSelectLeft.xaml.cs）。
/// 原版 MC 文件夹模型（GameFolders.McFolder）、右键菜单、拖拽排序、创建文件夹、导入整合包已裁剪；
/// 保留原版左栏骨架（分组标题 + 单选列表 + 底部添加按钮），条目映射为 KSP 实例根目录。
/// 选中仅切换当前实例并同步右栏勾选，不关闭页面；确认选择由右栏列表项点击完成。
/// </summary>
public partial class PageSelectLeft : IRefreshable, IPageView
{
    private readonly KspInstanceService _kspService = new();
    private bool _refreshed;

    public PageSelectLeft()
    {
        InitializeComponent();
        AnimatedControl = PanList;
        Loaded += PageSelectLeft_Loaded;
        Unloaded += PageSelectLeft_Unloaded;
        RefreshList();
    }

    public void Refresh() => RefreshList();

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom()
    {
    }

    private void RefreshOnNavigate()
    {
        if (_refreshed)
            return;
        _refreshed = true;
        RefreshList();
    }

    private void PageSelectLeft_Loaded(object sender, RoutedEventArgs e) => RefreshOnNavigate();

    private void PageSelectLeft_Unloaded(object sender, RoutedEventArgs e) => _refreshed = false;

    // 实例路径：当前选中 + 已保存列表（去重，保持顺序）
    private static List<string> GetInstancePaths()
    {
        var paths = new List<string>();
        var current = PklSettings.Current.KspRootPath;
        if (!string.IsNullOrWhiteSpace(current))
            paths.Add(current);
        foreach (var p in PklSettings.Current.Instances)
        {
            if (!string.IsNullOrWhiteSpace(p) &&
                !paths.Contains(p, StringComparer.OrdinalIgnoreCase))
                paths.Add(p);
        }

        return paths;
    }

    #region 结果 UI 化

    private void RefreshList()
    {
        PanList.Children.Clear();

        // 列表标题（对应原版 "Select.Folder.ListTitle"）
        PanList.Children.Add(new TextBlock
        {
            Text = "实例列表",
            Margin = new Thickness(13, 18, 5, 4),
            Opacity = 0.6,
            FontSize = 12
        });

        var paths = GetInstancePaths();
        foreach (var path in paths)
            PanList.Children.Add(CreateInstanceItem(path));

        // 添加区标题（对应原版 "Select.Folder.AddOrImport"）
        PanList.Children.Add(new TextBlock
        {
            Text = "添加实例",
            Margin = new Thickness(13, 18, 5, 4),
            Opacity = 0.6,
            FontSize = 12
        });

        // 添加已有实例按钮（对应原版 "添加已有文件夹"；创建文件夹 / 导入整合包为 MC 专属，已裁剪）
        var addItem = new MyListItem
        {
            IsScaleAnimationEnabled = false,
            Type = MyListItem.CheckType.Clickable,
            Title = "添加已有实例",
            Info = "选择本机 KSP 安装目录",
            Height = 34,
            SvgIcon = "lucide/folder-input",
            LogoScale = 0.9
        };
        ToolTipService.SetPlacement(addItem, PlacementMode.Right);
        addItem.Click += OnAddClick;
        PanList.Children.Add(addItem);

        PanList.Children.Add(new FrameworkElement { Height = 10, IsHitTestVisible = false });
    }

    private MyListItem CreateInstanceItem(string path)
    {
        var title = SafeName(path);
        string? version = null;
        try
        {
            version = _kspService.Probe(path)?.Version;
        }
        catch
        {
            // 版本探测失败不影响列表展示。
        }

        var item = new MyListItem
        {
            IsScaleAnimationEnabled = false,
            Type = MyListItem.CheckType.RadioBox,
            MinPaddingRight = 30,
            Title = title,
            Info = string.IsNullOrEmpty(version) ? path : $"KSP {version}  ·  {path}",
            SvgIcon = "lucide/server",
            LogoScale = 1.05d,
            tag = path,
            Height = 40d,
            Checked = string.Equals(path, PklSettings.Current.KspRootPath,
                StringComparison.OrdinalIgnoreCase)
        };
        item.Changed += OnInstanceChanged;
        return item;
    }

    private static string SafeName(string path)
    {
        try
        {
            var n = Path.GetFileName(path.TrimEnd('\\'));
            return string.IsNullOrEmpty(n) ? path : n;
        }
        catch
        {
            return path;
        }
    }

    #endregion

    #region 页面事件

    // 选中实例：写入选中实例并同步右栏勾选（不关闭页面）
    private void OnInstanceChanged(object sender, Common.RouteEventArgs e)
    {
        if (sender is not MyListItem item || item.tag is not string path)
            return;

        PklSettings.Current.KspRootPath = path;
        if (!PklSettings.Current.Instances.Contains(path, StringComparer.OrdinalIgnoreCase))
            PklSettings.Current.Instances.Add(path);
        PklSettings.Save();

        if (Launcher.frmMain?.pageRight is PageSelectRight right)
            right.RefreshList();
    }

    // 添加已有实例：选择 KSP 安装目录并加入实例列表
    private void OnAddClick(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "添加 KSP 实例目录" };
        if (dialog.ShowDialog() != true)
            return;

        var path = dialog.FolderName;
        if (!PklSettings.Current.Instances.Contains(path, StringComparer.OrdinalIgnoreCase))
            PklSettings.Current.Instances.Add(path);
        PklSettings.Current.KspRootPath = path;
        PklSettings.Save();

        RefreshList();
        if (Launcher.frmMain?.pageRight is PageSelectRight right)
            right.RefreshList();
    }

    #endregion
}
