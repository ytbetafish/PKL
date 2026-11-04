using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace PCL.Pages;

public partial class PageKspInstance
{
    public PageKspInstance()
    {
        InitializeComponent();
        RefreshInstancesList();
        RefreshKspInfo();
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
                RefreshKspInfo();
            }
        };
    }

    private void RefreshKspInfo()
    {
        var root = PklSettings.Current.KspRootPath;
        LabKspPath.Text = string.IsNullOrEmpty(root) ? "（未设置）" : root;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            LabKspVersion.Text = "—";
            LabKspExe.Text = "—";
            LabKspGameData.Text = "—";
            LabKspLogs.Text = "—";
            return;
        }
        try
        {
            var buildId = Path.Combine(root, "KSP_Data", "buildID.txt");
            var versionTxt = Path.Combine(root, "version.txt");
            if (File.Exists(buildId))
                LabKspVersion.Text = File.ReadAllText(buildId).Trim();
            else if (File.Exists(versionTxt))
                LabKspVersion.Text = File.ReadAllText(versionTxt).Trim();
            else
                LabKspVersion.Text = "未知";
        } catch { LabKspVersion.Text = "读取失败"; }
        LabKspExe.Text = File.Exists(Path.Combine(root, "KSP_x64.exe")) ? Path.Combine(root, "KSP_x64.exe") :
                         File.Exists(Path.Combine(root, "ksp.exe")) ? Path.Combine(root, "ksp.exe") : "未找到";
        var gd = Path.Combine(root, "GameData");
        LabKspGameData.Text = Directory.Exists(gd) ? gd : "不存在";
        var logs = Path.Combine(root, "KSP_Data", "output_log.txt");
        LabKspLogs.Text = File.Exists(logs) ? logs : Path.Combine(root, "KSP_Data");
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
            RefreshInstancesList();
            RefreshKspInfo();
        }
    }
}
