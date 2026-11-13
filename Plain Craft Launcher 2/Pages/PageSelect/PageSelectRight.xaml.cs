using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Pkl.Ckan;

namespace PCL.Pages;

/// <summary>
/// KSP 实例选择页（对应原版 InstanceSelect）。骨架复制自原版 PageSelectRight：
/// 搜索框 + 滚动列表 + 无实例空态 + 搜索无结果空态；数据层替换为 PklSettings.Instances
/// 与 KspInstanceService 探测，裁剪原版 MC 文件夹分组、版本分组卡片与 Loader 刷新逻辑。
/// </summary>
public partial class PageSelectRight
{
    public PageSelectRight()
    {
        InitializeComponent();
        RefreshList(string.Empty);
    }

    private List<string> GetInstancePaths()
    {
        var paths = new List<string>();
        var current = PklSettings.Current.KspRootPath;
        if (!string.IsNullOrWhiteSpace(current))
            paths.Add(current);
        foreach (var p in PklSettings.Current.Instances)
        {
            if (!string.IsNullOrWhiteSpace(p) && !paths.Contains(p))
                paths.Add(p);
        }
        return paths;
    }

    private void RefreshList(string search)
    {
        var all = GetInstancePaths();
        var filter = (search ?? string.Empty).Trim();

        PanEmpty.Visibility = all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var filtered = all;
        if (!string.IsNullOrEmpty(filter))
            filtered = all.FindAll(p => p.Contains(filter, StringComparison.OrdinalIgnoreCase));

        PanEmptySearch.Visibility = all.Count > 0 && filtered.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        PanMain.Children.Clear();
        var service = new KspInstanceService();
        foreach (var path in filtered)
            PanMain.Children.Add(BuildItem(path, service));
    }

    private MyListItem BuildItem(string path, KspInstanceService service)
    {
        string title;
        try
        {
            title = Path.GetFileName(path.TrimEnd('\\'));
        }
        catch
        {
            title = path;
        }
        if (string.IsNullOrEmpty(title))
            title = path;

        string? version;
        try
        {
            version = service.Probe(path)?.Version;
        }
        catch
        {
            version = null;
        }

        var info = string.IsNullOrEmpty(version) ? path : $"KSP {version}  ·  {path}";

        var item = new MyListItem
        {
            Title = title,
            Info = info,
            tag = path,
            Type = MyListItem.CheckType.Clickable,
            Height = 42,
            Checked = string.Equals(path, PklSettings.Current.KspRootPath, StringComparison.OrdinalIgnoreCase)
        };
        item.Click += OnInstanceClick;
        return item;
    }

    private void OnInstanceClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not MyListItem item || item.tag is not string path)
            return;

        PklSettings.Current.KspRootPath = path;
        PklSettings.Save();
        if (Window.GetWindow(item) is FormMain fm)
            fm.PageBack();
    }

    private void BtnEmptyAdd_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "添加 KSP 实例目录" };
        if (dialog.ShowDialog() != true)
            return;

        var path = dialog.FolderName;
        if (!PklSettings.Current.Instances.Contains(path))
            PklSettings.Current.Instances.Add(path);
        PklSettings.Current.KspRootPath = path;
        PklSettings.Save();
        RefreshList(PanVerSearchBox.Text);
    }

    private void PanSearchBox_TextChanged(object sender, EventArgs e)
    {
        RefreshList(PanVerSearchBox.Text);
    }
}
