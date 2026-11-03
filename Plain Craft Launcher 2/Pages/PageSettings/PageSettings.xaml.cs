using Microsoft.Win32;
using System.Windows;

namespace PCL.Pages;

public partial class PageSettings
{
    public PageSettings()
    {
        InitializeComponent();
        TxtBaseUrl.Text = PklSettings.Current.TranslateBaseUrl;
        TxtApiKey.Text = PklSettings.Current.TranslateApiKey;
        TxtModel.Text = PklSettings.Current.TranslateModel;
        LabKspPath.Text = string.IsNullOrWhiteSpace(PklSettings.Current.KspRootPath)
            ? "未设置" : PklSettings.Current.KspRootPath;
    }

    private void BtnPickKspPath_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择 KSP 安装目录（包含 KSP_x64.exe 与 GameData）"
        };
        if (dialog.ShowDialog() == true)
        {
            PklSettings.Current.KspRootPath = dialog.FolderName;
            PklSettings.Save();
            LabKspPath.Text = dialog.FolderName;
            LabSaved.Text = "KSP 路径已保存。";
        }
    }

    private void BtnSave_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PklSettings.Current.TranslateBaseUrl = TxtBaseUrl.Text.Trim();
        PklSettings.Current.TranslateApiKey = TxtApiKey.Text.Trim();
        PklSettings.Current.TranslateModel = TxtModel.Text.Trim();
        PklSettings.Save();
        LabSaved.Text = "已保存。";
    }
}
