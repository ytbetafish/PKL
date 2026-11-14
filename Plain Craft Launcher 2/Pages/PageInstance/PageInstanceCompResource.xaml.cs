using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Pkl.Ckan;

namespace PCL;

/// <summary>
/// KSP 实例设置页 · 模组（复刻原版 PageInstanceCompResource 本地资源管理器骨架）。
/// 原版 MC 的 .jar 扫描、启用禁用、更新、删除、导出、JarInJar、多选、资源包/光影/投影分支已裁剪。
/// 列表数据源为 CKAN 注册表的真实已装模组（名称、作者、版本、图标），对应原版 LocalCompFile 的
/// Name / Authors / Version / Logo / Description 字段；卡片行沿用原版通用控件 MyListItem。
/// </summary>
public partial class PageInstanceCompResource : IPageView
{
    private static ModService? _modService;
    private readonly List<ModSearchResult> _results = new();
    private string? _query;
    private bool _refreshed;
    private bool _serviceAttaching;

    public PageInstanceCompResource()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshOnNavigate();
        Unloaded += (_, _) => _refreshed = false;
    }

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom()
    {
    }

    private static ModService GetService()
    {
        ModService.ApplyNetworkSettings(
            PklSettings.Current.HttpProxy,
            PklSettings.Current.DownloadConcurrency,
            PklSettings.Current.HttpTimeoutSeconds);
        if (_modService is null)
        {
            _modService = new ModService(dataDir: PklSettings.Current.CkanDataDir);
        }
        _modService.MirrorUrls = new List<string>(PklSettings.Current.CkanMirrorUrls);
        _modService.OfficialRepoUrl = PklSettings.Current.CkanMasterRepoUrl;
        _modService.RepoStrategy = PklSettings.Current.CkanRepoStrategy;
        return _modService;
    }

    private async void RefreshOnNavigate()
    {
        if (_refreshed)
            return;
        _refreshed = true;

        PanLoad.Visibility = Visibility.Visible;
        PanListCard.Visibility = Visibility.Collapsed;
        PanEmpty.Visibility = Visibility.Collapsed;

        try
        {
            await AttachInstanceAsync();
            var list = await GetService().ListInstalledAsync();
            _results.Clear();
            _results.AddRange(list);
        }
        catch (Exception ex)
        {
            Common.Log(ex, "模组列表加载失败");
            _results.Clear();
        }

        RenderList();
        PanLoad.Visibility = Visibility.Collapsed;
    }

    private async Task AttachInstanceAsync()
    {
        if (_serviceAttaching)
            return;
        _serviceAttaching = true;

        var root = PklSettings.Current.KspRootPath;
        if (string.IsNullOrWhiteSpace(root) || !KspInstanceService.IsKspRoot(root))
            return;

        await Task.Run(() =>
        {
            var ksp = new KspInstanceService();
            GetService().AttachInstanceRobust(ksp.Load(root));
        });
    }

    private void SearchRun(object sender, EventArgs e)
    {
        _query = SearchBox.Text?.Trim();
        RenderList();
    }

    private void RenderList()
    {
        PanList.Children.Clear();

        IEnumerable<ModSearchResult> showing = _results;
        if (!string.IsNullOrWhiteSpace(_query))
        {
            showing = showing.Where(m =>
                m.Name.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                m.Identifier.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                (m.Author?.Contains(_query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = showing.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();

        if (list.Count == 0)
        {
            PanListCard.Visibility = Visibility.Collapsed;
            PanEmpty.Visibility = Visibility.Visible;
            TxtEmptyDescription.Text = string.IsNullOrEmpty(PklSettings.Current.KspRootPath)
                ? "未选择 KSP 实例"
                : "当前实例尚未通过 CKAN 安装模组";
            return;
        }

        PanEmpty.Visibility = Visibility.Collapsed;
        PanListCard.Visibility = Visibility.Visible;
        foreach (var mod in list)
            PanList.Children.Add(CreateItem(mod));
    }

    private static readonly Brush[] _initialBrushes =
    [
        new SolidColorBrush(Color.FromRgb(0x5B, 0x6E, 0xE1)),
        new SolidColorBrush(Color.FromRgb(0x3D, 0x99, 0x70)),
        new SolidColorBrush(Color.FromRgb(0xE8, 0x83, 0x3A)),
        new SolidColorBrush(Color.FromRgb(0xC8, 0x50, 0x6B)),
        new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xA4)),
        new SolidColorBrush(Color.FromRgb(0x8E, 0x7C, 0xC3))
    ];

    private MyCompItem CreateItem(ModSearchResult mod)
    {
        var info = new List<string>();
        if (!string.IsNullOrWhiteSpace(mod.Author))
            info.Add(mod.Author!);
        if (!string.IsNullOrWhiteSpace(mod.Version))
            info.Add(mod.Version!);
        info.Add(mod.HasUpdate ? "有更新" : "已安装");

        var tooltipLines = new List<string>();
        if (mod.Depends is { Count: > 0 })
        {
            info.Add($"前置 {mod.Depends.Count}");
            tooltipLines.Add(FormatRelList("依赖：", mod.Depends)!);
        }
        if (mod.ReverseDeps is { Count: > 0 })
        {
            info.Add($"被依赖 {mod.ReverseDeps.Count}");
            tooltipLines.Add(FormatRelList("被依赖：", mod.ReverseDeps)!);
        }
        if (mod.Conflicts is { Count: > 0 })
        {
            info.Add($"冲突 {mod.Conflicts.Count}");
            tooltipLines.Add(FormatRelList("冲突：", mod.Conflicts)!);
        }

        var project = CkanCompAdapter.ToProject(mod);
        var item = new MyCompItem { Tag = project };
        item.ShowFavoriteBtn = true;
        item.Title = mod.Name;
        item.Description = string.Join(" · ", info);
        item.ShowMetaRow = false;
        if (tooltipLines.Count > 0)
        {
            item.ToolTip = new ToolTip
            {
                MaxWidth = 360,
                Content = new TextBlock
                {
                    Text = string.Join(Environment.NewLine, tooltipLines),
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        if (IsImageUrl(mod.IconUrl))
        {
            item.Logo = mod.IconUrl!;
            item.ShowLetter = false;
        }
        else
        {
            item.ShowLetter = true;
            item.Letter = GetInitial(mod.Name);
            item.LetterBrush = GetInitialBrush(mod.Name);
        }
        item.RefreshFavoriteStatus();
        return item;
    }

    private const int RelTooltipMaxNames = 5;

    private static string? FormatRelList(string prefix, IReadOnlyList<string>? names)
    {
        if (names is not { Count: > 0 }) return null;
        var text = prefix + string.Join("、", names.Take(RelTooltipMaxNames));
        if (names.Count > RelTooltipMaxNames)
            text += "（等 " + names.Count + " 个）";
        return text;
    }

    private static string GetInitial(string? name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            return "?";
        var ch = name[0];
        return char.IsLetter(ch) ? char.ToUpperInvariant(ch).ToString() : ch.ToString();
    }

    private static Brush GetInitialBrush(string key)
    {
        var hash = 0;
        foreach (var c in key ?? string.Empty)
            hash = unchecked(hash * 31 + c);
        return _initialBrushes[Math.Abs(hash) % _initialBrushes.Length];
    }

    private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e) => OpenGameData();

    private static bool IsImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return true;
        return url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveGameData()
    {
        var settings = PklSettings.Current;
        if (!string.IsNullOrWhiteSpace(settings.KspGameDataPath) && Directory.Exists(settings.KspGameDataPath))
            return settings.KspGameDataPath;
        var root = settings.KspRootPath;
        return string.IsNullOrWhiteSpace(root) ? null : Path.Combine(root, "GameData");
    }

    private static void OpenGameData()
    {
        var gameData = ResolveGameData();
        if (!string.IsNullOrEmpty(gameData) && Directory.Exists(gameData))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = gameData,
                UseShellExecute = true
            });
    }
}
