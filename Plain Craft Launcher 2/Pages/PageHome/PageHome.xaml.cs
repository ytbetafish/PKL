using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Pkl.Ckan;

namespace PCL.Pages;

public partial class PageHome : UserControl
{
    public PageHome()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshPath();
    }

    private void RefreshPath()
    {
        var path = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            LabKspPath.Text = "未选择";
            LabKspVersion.Text = "";
            return;
        }
        LabKspPath.Text = path;
        try
        {
            var probe = new KspInstanceService().Probe(path);
            LabKspVersion.Text = probe?.Version is null ? "（未检测到 KSP 版本）" : $"检测到 KSP 版本：{probe.Version}";
        }
        catch
        {
            LabKspVersion.Text = "（路径无效）";
        }
    }

    private void BtnPickPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择 KSP 安装目录（包含 KSP_x64.exe 与 GameData）"
        };
        if (dialog.ShowDialog() == true)
        {
            PklSettings.Current.KspRootPath = dialog.FolderName;
            PklSettings.Save();
            RefreshPath();
        }
    }

    private void BtnLaunch_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PklSettings.Current.KspRootPath) ||
            !KspInstanceService.IsKspRoot(PklSettings.Current.KspRootPath))
        {
            MessageBox.Show("请先选择有效的 KSP 安装路径。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        MessageBox.Show("启动 KSP 功能尚未实现（占位）。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
