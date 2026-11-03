using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using Pkl.Ckan;

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

        LabCkanDataDir.Text = PklSettings.Current.CkanDataDir;
        TxtCkanRepoUrl.Text = PklSettings.Current.CkanMasterRepoUrl;
        switch (PklSettings.Current.CkanStabilityTolerance)
        {
            case "Testing": OptTesting.Checked = true; break;
            case "PreRelease": OptPre.Checked = true; break;
            default: OptStable.Checked = true; break;
        }
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

    private void BtnPickCkanDir_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择 CKAN 数据目录（保存索引缓存）"
        };
        if (dialog.ShowDialog() == true)
        {
            PklSettings.Current.CkanDataDir = dialog.FolderName;
            PklSettings.Save();
            LabCkanDataDir.Text = dialog.FolderName;
            LabCkanStatus.Text = "数据目录已保存，重启 PKL 后生效。";
        }
    }

    private async void BtnRefreshRepo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            BtnRefreshRepo.IsEnabled = false;
            LabCkanStatus.Text = "正在刷新模组索引…";
            var svc = new ModService(dataDir: PklSettings.Current.CkanDataDir);
            await svc.RefreshMasterRegistryAsync();
            LabCkanStatus.Text = "索引刷新完成。";
        }
        catch (Exception ex)
        {
            LabCkanStatus.Text = "刷新失败：" + ex.Message;
        }
        finally
        {
            BtnRefreshRepo.IsEnabled = true;
        }
    }

    private void BtnSave_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PklSettings.Current.TranslateBaseUrl = TxtBaseUrl.Text.Trim();
        PklSettings.Current.TranslateApiKey = TxtApiKey.Text.Trim();
        PklSettings.Current.TranslateModel = TxtModel.Text.Trim();
        PklSettings.Current.CkanMasterRepoUrl = TxtCkanRepoUrl.Text.Trim();
        PklSettings.Current.CkanStabilityTolerance =
            OptPre.Checked ? "PreRelease" : OptTesting.Checked ? "Testing" : "Stable";
        PklSettings.Save();
        LabSaved.Text = "已保存。";
    }
}
