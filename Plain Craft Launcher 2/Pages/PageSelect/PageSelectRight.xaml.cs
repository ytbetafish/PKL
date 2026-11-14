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
/// KSP 实例选择页右侧（对应原版 PageSelectRight.xaml.cs）。
/// 原版的 MC 版本分组类型、LoaderTask、收藏 / 删除 / 隐藏 / 打包安装已裁剪；
/// 保留原版页面骨架与列表项结构：分组卡片（MyCard）+ 列表项（MyListItem，图标 / 标签 / 右侧按钮）
/// + 搜索框 + 无实例空态 + 搜索无结果空态。
/// </summary>
public partial class PageSelectRight : IPageView
{
    private readonly PageSelectRightViewModel _vm = new();
    private readonly KspInstanceService _kspService = new();
    private bool _refreshed;

    public PageSelectRight()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += PageSelectRight_Loaded;
        Unloaded += PageSelectRight_Unloaded;
        RefreshList();
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
        PanBack.ScrollToHome();
        RefreshList();
    }

    private void PageSelectRight_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshOnNavigate();
        PanVerSearchBox.TextChanged += PanVerSearchBox_TextChanged;
    }

    private void PageSelectRight_Unloaded(object sender, RoutedEventArgs e) => _refreshed = false;

    private void PanVerSearchBox_TextChanged(object sender, EventArgs e) => RefreshList();

    // 实例路径：当前选中 + 已保存列表（去重，保持顺序）
    private List<string> GetInstancePaths()
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

    public void RefreshList()
    {
        var all = GetInstancePaths();
        var search = PanVerSearchBox.Text.Trim();
        var filtered = all.FindAll(p => PageSelectRightViewModel.MatchesSearch(p, search));

        _vm.EmptyPanelVisibility = all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _vm.EmptySearchPanelVisibility = all.Count > 0 && filtered.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _vm.SearchBoxVisibility = all.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _vm.BackVisibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        PanMain.Children.Clear();
        if (filtered.Count == 0)
            return;

        // 分组卡片（对应原版 MyCard 分组折叠卡片；KSP 下仅一个分组）
        var newCard = new MyCard
        {
            Title = "KSP 实例",
            Margin = new Thickness(0d, 0d, 0d, 15d)
        };
        var newStack = new StackPanel
        {
            Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 0d),
            VerticalAlignment = VerticalAlignment.Top,
            Tag = filtered
        };
        newCard.Children.Add(newStack);
        newCard.SwapControl = newStack;
        newCard.InstallMethod = stack =>
        {
            foreach (var path in (List<string>)stack.Tag)
                stack.Children.Add(CreateInstanceItem(path));
        };
        PanMain.Children.Add(newCard);
        newCard.StackInstall();
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

        var tags = new List<string>();
        if (!string.IsNullOrEmpty(version))
            tags.Add(version);

        var item = new MyListItem
        {
            Title = title,
            Info = string.IsNullOrEmpty(version) ? path : $"KSP {version}  ·  {path}",
            PlainTags = tags,
            SvgIcon = "lucide/server",
            LogoScale = 1.05d,
            tag = path,
            Height = 42d,
            Type = MyListItem.CheckType.Clickable,
            Checked = string.Equals(path, PklSettings.Current.KspRootPath,
                StringComparison.OrdinalIgnoreCase)
        };
        item.ContentHandler = (sender, e) => InstanceItemContent(item, path);
        item.Click += OnInstanceClick;
        return item;
    }

    // 右侧操作按钮（对应原版 McVersionListContent；收藏 / 删除 / 隐藏为 MC 专属，已裁剪）
    private void InstanceItemContent(MyListItem item, string path)
    {
        var btnOpenFolder = new MyIconButton { LogoScale = 1.1d, SvgIcon = "lucide/folder-open" };
        btnOpenFolder.ToolTip = "打开实例目录";
        ToolTipService.SetPlacement(btnOpenFolder, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnOpenFolder, 30d);
        ToolTipService.SetHorizontalOffset(btnOpenFolder, 2d);
        btnOpenFolder.Click += (_, _) => OpenFolder(path);

        item.Buttons = new[] { btnOpenFolder };
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

    // 点击选项：写入选中实例并返回
    private void OnInstanceClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not MyListItem item || item.tag is not string path)
            return;

        PklSettings.Current.KspRootPath = path;
        if (!PklSettings.Current.Instances.Contains(path, StringComparer.OrdinalIgnoreCase))
            PklSettings.Current.Instances.Add(path);
        PklSettings.Save();

        if (Window.GetWindow(item) is FormMain fm)
            fm.PageBack();
    }

    // 空态按钮：选择 KSP 安装目录并加入实例列表
    private void BtnEmptyAdd_Click(object sender, MouseButtonEventArgs e)
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
    }

    #endregion
}
