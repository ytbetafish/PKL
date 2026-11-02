using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PCL.Translation;
using Pkl.Ckan;

namespace PCL.Pages;

public partial class PageMods : UserControl
{
    private static ModService? _modService;
    private readonly Dictionary<string, string?> _transCache = new();
    private ModSearchResult? _selected;
    private bool _showChinese;
    private string? _originalDescription;

    public PageMods()
    {
        InitializeComponent();
        Loaded += async (_, _) => await EnsureServiceAndRefresh();
    }

    private static ModService GetService()
    {
        if (_modService is null)
        {
            _modService = new ModService();
            var root = PklSettings.Current.KspRootPath;
            if (!string.IsNullOrWhiteSpace(root) && KspInstanceService.IsKspRoot(root))
            {
                try
                {
                    var ksp = new KspInstanceService();
                    _modService.AttachInstance(ksp.Load(root));
                }
                catch
                {
                    // 附加失败则保持无实例，搜索仍可用。
                }
            }
        }
        return _modService;
    }

    private async Task EnsureServiceAndRefresh()
    {
        try
        {
            await GetService().RefreshMasterRegistryAsync();
            await DoSearch(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show("加载模组注册表失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DoSearch(string? query)
    {
        try
        {
            var results = await GetService().SearchAsync(query);
            GridMods.ItemsSource = results;
        }
        catch (Exception ex)
        {
            MessageBox.Show("搜索失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await DoSearch(TxtSearch.Text);
    private async void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            await DoSearch(TxtSearch.Text);
    }

    private async void BtnInstalled_Click(object sender, RoutedEventArgs e)
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

    private async void BtnTranslate_Click(object sender, RoutedEventArgs e)
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

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        try
        {
            var plan = await GetService().GetInstallPlanAsync(_selected.Identifier);
            if (plan is null)
            {
                MessageBox.Show("无法为该模组生成安装计划（未附加 KSP 实例？）。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var progress = new Progress<string>(msg => LabModDesc.Text = msg);
            await GetService().ExecutePlanAsync(plan, progress);
            MessageBox.Show("安装计划执行完成。", "PKL", MessageBoxButton.OK, MessageBoxImage.Information);
            await DoSearch(TxtSearch.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show("安装失败：" + ex.Message, "PKL", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
