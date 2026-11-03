using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
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

        TxtProxy.Text = PklSettings.Current.HttpProxy;
        TxtConcurrency.Text = PklSettings.Current.DownloadConcurrency.ToString();
        TxtTimeout.Text = PklSettings.Current.HttpTimeoutSeconds.ToString();

        RefreshInstancesList();
    }

    private void RefreshInstancesList()
    {
        CboInstances.Items.Clear();
        foreach (var p in PklSettings.Current.Instances) CboInstances.Items.Add(p);
        if (!string.IsNullOrEmpty(PklSettings.Current.KspRootPath)
            && !PklSettings.Current.Instances.Contains(PklSettings.Current.KspRootPath))
        {
            CboInstances.Items.Insert(0, PklSettings.Current.KspRootPath);
        }
        CboInstances.SelectedItem = PklSettings.Current.KspRootPath;
        CboInstances.SelectionChanged += (_, _) =>
        {
            if (CboInstances.SelectedItem is string s && !string.IsNullOrEmpty(s))
            {
                PklSettings.Current.KspRootPath = s;
                PklSettings.Save();
                LabKspPath.Text = s;
                LabSaved.Text = "已切换 KSP 实例，重启 PKL 后生效。";
            }
        };
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
            if (!PklSettings.Current.Instances.Contains(dialog.FolderName))
                PklSettings.Current.Instances.Add(dialog.FolderName);
            PklSettings.Save();
            LabKspPath.Text = dialog.FolderName;
            LabSaved.Text = "KSP 路径已保存。";
            RefreshInstancesList();
        }
    }

    private void BtnAddInstance_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "添加 KSP 实例目录" };
        if (dialog.ShowDialog() == true)
        {
            if (!PklSettings.Current.Instances.Contains(dialog.FolderName))
                PklSettings.Current.Instances.Add(dialog.FolderName);
            PklSettings.Current.KspRootPath = dialog.FolderName;
            PklSettings.Save();
            LabKspPath.Text = dialog.FolderName;
            LabSaved.Text = "实例已添加，重启 PKL 后生效。";
            RefreshInstancesList();
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

    private async void BtnExportMods_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            var svc = new ModService(dataDir: PklSettings.Current.CkanDataDir);
            var ksp = new KspInstanceService();
            svc.AttachInstance(ksp.Load(PklSettings.Current.KspRootPath));
            var installed = await svc.ListInstalledAsync();
            var rows = installed.Select(m => new { name = m.Identifier, version = m.Version ?? "" }).ToList();
            var dlg = new SaveFileDialog { Filter = "CKAN 清单|*.ckan|JSON|*.json", FileName = "pkl-modlist.ckan" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                LabCkanStatus.Text = $"已导出 {rows.Count} 个已安装模组到 {dlg.FileName}";
            }
        }
        catch (Exception ex)
        {
            LabCkanStatus.Text = "导出失败：" + ex.Message;
        }
    }

    private async void BtnImportMods_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            var dlg = new OpenFileDialog { Filter = "CKAN 清单|*.ckan;*.json" };
            if (dlg.ShowDialog() != true) return;
            var json = File.ReadAllText(dlg.FileName);
            var rows = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ImportRow>>(json);
            if (rows is not { Count: > 0 }) { LabCkanStatus.Text = "清单为空。"; return; }

            var svc = new ModService(dataDir: PklSettings.Current.CkanDataDir);
            var ksp = new KspInstanceService();
            svc.AttachInstance(ksp.Load(PklSettings.Current.KspRootPath));

            var progress = new Progress<string>(msg => LabCkanStatus.Text = msg);
            foreach (var row in rows)
            {
                LabCkanStatus.Text = $"正在安装 {row.name}…";
                var plan = await svc.GetInstallPlanAsync(row.name);
                if (plan is not null) await svc.ExecutePlanAsync(plan, progress);
            }
            LabCkanStatus.Text = $"清单导入完成（{rows.Count} 项）。";
        }
        catch (Exception ex)
        {
            LabCkanStatus.Text = "导入失败：" + ex.Message;
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
        PklSettings.Current.HttpProxy = TxtProxy.Text.Trim();
        int.TryParse(TxtConcurrency.Text.Trim(), out var conc);
        if (conc > 0) PklSettings.Current.DownloadConcurrency = conc;
        int.TryParse(TxtTimeout.Text.Trim(), out var to);
        if (to > 0) PklSettings.Current.HttpTimeoutSeconds = to;
        PklSettings.Save();
        LabSaved.Text = "已保存。";
    }

    private sealed class ImportRow
    {
        public string name { get; set; } = "";
        public string version { get; set; } = "";
    }
}
