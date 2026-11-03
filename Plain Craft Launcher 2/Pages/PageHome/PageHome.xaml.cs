using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Pkl.Ckan;

namespace PCL.Pages;

public partial class PageHome
{
    private Process? _kspProcess;

    public PageHome()
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
            LabKspPath.Text = "未设置 KSP 路径（请到设置页选择）";
            LabKspVersion.Text = "";
            return;
        }
        LabKspPath.Text = path;
        try
        {
            var probe = new KspInstanceService().Probe(path);
            LabKspVersion.Text = probe?.Version is null ? "未检测到 KSP" : $"KSP {probe.Version}";
        }
        catch
        {
            LabKspVersion.Text = "（路径无效）";
        }
    }

    private void BtnLaunch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var root = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(root) || !KspInstanceService.IsKspRoot(root))
        {
            MessageBox.Show("请先选择有效的 KSP 安装路径。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 已在运行：点击按钮 → 提示
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
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = root,
                UseShellExecute = true
            };
            _kspProcess = Process.Start(psi);
            if (_kspProcess is not null)
            {
                _kspProcess.EnableRaisingEvents = true;
                _kspProcess.Exited += OnKspExited;
                SetLaunchedUi();
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
            SetIdleUi();
        }));
    }

    private void SetLaunchedUi()
    {
        BtnLaunch.Text = "游戏运行中…";
        BtnLaunch.IsEnabled = false;
        LabKspStatus.Text = $"KSP 已启动（PID {_kspProcess?.Id}）";
    }

    private void SetIdleUi()
    {
        BtnLaunch.Text = "启动 KSP";
        BtnLaunch.IsEnabled = true;
        LabKspStatus.Text = "";
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
