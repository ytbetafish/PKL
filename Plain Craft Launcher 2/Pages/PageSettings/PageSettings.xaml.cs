using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        LabCkanDataDir.Text = PklSettings.Current.CkanDataDir;
        TxtMirrorUrls.Text = string.Join("\n", PklSettings.Current.CkanMirrorUrls);
        foreach (System.Windows.Controls.ComboBoxItem item in CboStrategy.Items)
        {
            if ((string)item.Tag == PklSettings.Current.CkanRepoStrategy) { CboStrategy.SelectedItem = item; break; }
        }
        if (CboStrategy.SelectedItem == null) CboStrategy.SelectedIndex = 0;
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
        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;
        NavKsp.Checked = true;
    }

    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
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
                LabSaved.Text = "已切换 KSP 实例，重启 PKL 后生效。";
            }
        };
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
            svc.MirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            svc.OfficialRepoUrl = PklSettings.Current.CkanMasterRepoUrl;
            svc.RepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "OfficialFirst";
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

    private async void BtnTestRepo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var url = TxtMirrorUrls.Text.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).FirstOrDefault();
        if (string.IsNullOrEmpty(url)) { LabCkanStatus.Text = "未填镜像 URL。"; return; }
        if (string.IsNullOrWhiteSpace(url)) { LabCkanStatus.Text = "URL 为空。"; return; }
        try
        {
            LabCkanStatus.Text = "正在测试连接…";
            using var http = new System.Net.Http.HttpClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            LabCkanStatus.Text = $"连接成功：HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase})";
        }
        catch (Exception ex)
        {
            LabCkanStatus.Text = $"连接失败：{ex.Message}";
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
        PklSettings.Current.CkanMirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        PklSettings.Current.CkanRepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "MirrorOnly";
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
