using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PCL.Translation;
using Pkl.Ckan;

namespace PCL.Pages;

public partial class PageMods
{
    private static ModService? _modService;
    private readonly Dictionary<string, string?> _transCache = new();
    private ModSearchResult? _selected;
    private bool _showChinese;
    private string? _originalDescription;
    private bool _serviceAttaching;

    public PageMods()
    {
        InitializeComponent();
        // 不在构造函数/Loaded 做任何同步 IO；实例附加放到后台，搜索由用户触发。
        Loaded += async (_, _) => await AttachKspBackgroundAsync();
    }

    private static ModService GetService()
    {
        if (_modService is null)
        {
            _modService = new ModService(dataDir: PklSettings.Current.CkanDataDir);
        }
        return _modService;
    }

    private async Task AttachKspBackgroundAsync()
    {
        if (_serviceAttaching) return;
        _serviceAttaching = true;
        try
        {
            var root = PklSettings.Current.KspRootPath;
            if (string.IsNullOrWhiteSpace(root) || !KspInstanceService.IsKspRoot(root)) return;
            await Task.Run(() =>
            {
                var ksp = new KspInstanceService();
                GetService().AttachInstance(ksp.Load(root));
            });
        }
        catch (Exception ex)
        {
            LabModDesc.Text = "KSP 实例附加失败：" + ex.Message;
        }
    }

    private async Task DoSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            LabModDesc.Text = "输入关键词后按回车或点搜索。";
            return;
        }
        try
        {
            GridMods.ItemsSource = await GetService().SearchAsync(query);
        }
        catch (Exception ex)
        {
            MessageBox.Show("搜索失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnSearch_Click(object sender, EventArgs e) => await DoSearch(TxtSearch.Text);

    private async void BtnInstalled_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            GridMods.ItemsSource = await GetService().ListInstalledAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("列出已安装模组失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GridMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = GridMods.SelectedItem as ModSearchResult;
        _showChinese = false;
        if (_selected is null)
        {
            LabModName.Text = "（选中一个模组）";
            LabModDesc.Text = "";
            LabModHome.Text = "";
            BtnInstall.Visibility = Visibility.Collapsed;
            BtnTranslate.Visibility = Visibility.Collapsed;
            return;
        }
        LabModName.Text = _selected.Name;
        _originalDescription = _selected.Description;
        LabModDesc.Text = string.IsNullOrWhiteSpace(_originalDescription) ? "（无简介）" : _originalDescription;
        LabModHome.Text = string.IsNullOrWhiteSpace(_selected.Homepage) ? "" : _selected.Homepage;
        BtnInstall.Visibility = string.IsNullOrWhiteSpace(PklSettings.Current.KspRootPath) ? Visibility.Collapsed : Visibility.Visible;
        BtnTranslate.Visibility = string.IsNullOrWhiteSpace(_originalDescription) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void BtnTranslate_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selected is null || string.IsNullOrWhiteSpace(_selected.Description))
            return;

        _showChinese = !_showChinese;
        if (!_showChinese)
        {
            LabModDesc.Text = _originalDescription ?? "";
            return;
        }

        var key = _selected.Identifier;
        if (_transCache.TryGetValue(key, out var cached))
        {
            LabModDesc.Text = cached ?? "（翻译失败）";
            return;
        }

        var svc = new OpenAiTranslationService(new TranslationOptions
        {
            BaseUrl = PklSettings.Current.TranslateBaseUrl,
            ApiKey = PklSettings.Current.TranslateApiKey,
            Model = PklSettings.Current.TranslateModel
        });
        LabModDesc.Text = "翻译中…";
        var result = await svc.TranslateAsync(_selected.Description, "zh-CN");
        _transCache[key] = result;
        LabModDesc.Text = result ?? "（翻译失败，请检查翻译设置）";
    }

    private async void BtnInstall_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selected is null) return;
        try
        {
            PanProgress.Visibility = Visibility.Visible;
            BarProgress.IsIndeterminate = true;
            LabProgress.Text = "正在解析依赖…";

            var plan = await GetService().GetInstallPlanAsync(_selected.Identifier);
            if (plan is null)
            {
                PanProgress.Visibility = Visibility.Collapsed;
                MessageBox.Show("无法为该模组生成安装计划（未附加 KSP 实例？）。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LabProgress.Text = $"计划：安装 {plan.ToInstall.Count} 个，升级 {plan.ToUpgrade.Count} 个。";
            var progress = new Progress<string>(msg =>
            {
                Dispatcher.BeginInvoke(new Action(() => LabProgress.Text = msg));
            });

            await GetService().ExecutePlanAsync(plan, progress);

            PanProgress.Visibility = Visibility.Collapsed;
            MessageBox.Show("安装计划执行完成。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            await DoSearch(TxtSearch.Text);
        }
        catch (Exception ex)
        {
            PanProgress.Visibility = Visibility.Collapsed;
            MessageBox.Show("安装失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
