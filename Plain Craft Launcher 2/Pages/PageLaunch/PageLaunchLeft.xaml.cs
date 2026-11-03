using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Pkl.Ckan;

namespace PCL;

public partial class PageLaunchLeft
{
    private Process? _kspProcess;

    public PageLaunchLeft()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshPath();
        Unloaded += (_, _) => DetachKspWatcher();
    }

    private void RefreshPath()
    {
        var path = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            LabKspVersion.Text = "未设置 KSP";
            LabKspPath.Text = "请到设置页选择 KSP 安装目录";
            return;
        }
        LabKspPath.Text = path;
        try
        {
            var probe = new KspInstanceService().Probe(path);
            LabKspVersion.Text = probe?.Version is null ? "KSP" : $"KSP {probe.Version}";
            LabVersion.Text = probe?.Version ?? "";
        }
        catch
        {
            LabKspVersion.Text = "KSP（路径无效）";
        }
    }

    private void BtnLaunch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var root = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(root) || !KspInstanceService.IsKspRoot(root))
        {
            MessageBox.Show("请先在设置页选择有效的 KSP 安装路径。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_kspProcess is { HasExited: false })
        {
            MessageBox.Show("KSP 已经在运行中。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var exe = Path.Combine(root, "KSP_x64.exe");
        if (!File.Exists(exe)) exe = Path.Combine(root, "KSP.exe");
        if (!File.Exists(exe))
        {
            MessageBox.Show("未找到 KSP_x64.exe / KSP.exe。", "PKL", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            var psi = new ProcessStartInfo { FileName = exe, WorkingDirectory = root, UseShellExecute = true };
            _kspProcess = Process.Start(psi);
            if (_kspProcess is not null)
            {
                _kspProcess.EnableRaisingEvents = true;
                _kspProcess.Exited += OnKspExited;
                BtnLaunch.Text = "游戏运行中…";
                BtnLaunch.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("启动失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnKspExited(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            DetachKspWatcher();
            BtnLaunch.Text = "启动 KSP";
            BtnLaunch.IsEnabled = true;
        }));
    }

    private void BtnOpenDir_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var root = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            MessageBox.Show("KSP 路径未设置或不存在。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo { FileName = root, UseShellExecute = true });
    }

    private void BtnGotoSettings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (Window.GetWindow(this) is FormMain fm)
            fm.SelectNavTag("2");
    }

    private void DetachKspWatcher()
    {
        if (_kspProcess is not null)
        {
            try { _kspProcess.Exited -= OnKspExited; } catch { }
            try { _kspProcess.Dispose(); } catch { }
            _kspProcess = null;
        }
    }
}
