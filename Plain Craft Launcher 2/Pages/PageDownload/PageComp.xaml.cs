using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;
using PCL.Core.UI.Hints;
using Pkl.Ckan;

namespace PCL;

[ContentProperty("SearchTags")]
public partial class PageComp : Grid
{
    private readonly PageCompViewModel _vm = new();

    private List<Comp.CompProject> _projects = new();
    private List<ModSearchResult> _results = new();
    private readonly HashSet<int> _enrichedPages = new();
    private CancellationTokenSource? _enrichCts;
    private int _currentPage = 0;
    private const int PageSize = 40;
    private bool _loading;

    public PageComp()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += PageComp_Loaded;
    }

    private async void PageComp_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProjects(null);
    }

    /// <summary>从 CKAN 注册表搜索模组并刷新列表。</summary>
    private async Task LoadProjects(string? keyword)
    {
        if (_loading)
            return;
        _loading = true;
        PanLoad.Visibility = Visibility.Visible;
        PanContent.Visibility = Visibility.Collapsed;
        _vm.ApplyErrorState(null);
        try
        {
            var query = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            var sw = Stopwatch.StartNew();
            Common.Log("[CKAN] SearchAsync 开始");
            var results = await Launcher.Ckan.SearchAsync(query);
            Common.Log($"[CKAN] SearchAsync 返回 {results.Count} 条，耗时 {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            _results = results.ToList();
            _projects = CkanCompAdapter.ToProjects(_results);
            Common.Log($"[CKAN] ToProjects {_projects.Count} 条，耗时 {sw.ElapsedMilliseconds} ms");
            _enrichedPages.Clear();
            _currentPage = 0;
            RefreshList();
            PanContent.Visibility = Visibility.Visible;
            Common.Log($"[CKAN] RefreshList 完成，卡片 {PanProjects.Children.Count} 个");
            _ = EnrichCurrentPageAsync();
        }
        catch (Exception ex)
        {
            Common.Log(ex, "CKAN 模组列表加载失败");
            PanContent.Visibility = Visibility.Visible;
            _vm.ApplyErrorState(ex.Message);
        }
        finally
        {
            PanLoad.Visibility = Visibility.Collapsed;
            _loading = false;
        }
    }

    private void RefreshList()
    {
        PanProjects.Children.Clear();
        var start = _currentPage * PageSize;
        var end = Math.Min(start + PageSize, _projects.Count);
        for (var i = start; i < end; i++)
            PanProjects.Children.Add(_projects[i].ToCompItem(false, false));
        _vm.ApplyPagerState(_currentPage, _projects.Count, PageSize);
    }

    /// <summary>异步回填当前页的 SpaceDock 信息（图标/下载量等）；已回填的页保留在本地缓存，不重复请求。</summary>
    private async Task EnrichCurrentPageAsync()
    {
        _enrichCts?.Cancel();
        _enrichCts?.Dispose();
        _enrichCts = new CancellationTokenSource();
        var ct = _enrichCts.Token;

        if (_results.Count == 0 || _enrichedPages.Contains(_currentPage))
            return;

        var start = _currentPage * PageSize;
        if (start >= _results.Count)
            return;
        var count = Math.Min(PageSize, _results.Count - start);

        try
        {
            await Launcher.Ckan.EnrichRangeAsync(_results, start, count, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested)
                return;

            _enrichedPages.Add(_currentPage);
            var mapped = CkanCompAdapter.ToProjects(_results.GetRange(start, count));
            for (var i = 0; i < mapped.Count; i++)
                _projects[start + i] = mapped[i];

            await Dispatcher.InvokeAsync(() => RefreshList());
        }
        catch (OperationCanceledException)
        {
            // 翻页取消，丢弃本次回填结果。
        }
        catch (Exception ex)
        {
            Common.Log(ex, "当前页模组信息回填失败");
        }
    }

    public ItemCollection SearchTags => ComboSearchTag.Items;

    public string TypeName => "模组";
    public string TypeNameSpaced => "模组";

    public object PageType { get; set; }

    private void PanSearchBox_Search(object sender, EventArgs e) => StartSearch();

    private void EnterTrigger(object sender, KeyEventArgs e) => StartSearch();

    private void BtnSearchReset_Click(object sender, EventArgs e) => ResetFilter();

    private void BtnPageFirst_Click(object sender, EventArgs e)
    {
        _currentPage = 0;
        RefreshList();
        _ = EnrichCurrentPageAsync();
    }

    private void BtnPageLeft_Click(object sender, EventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshList();
            _ = EnrichCurrentPageAsync();
        }
    }

    private void BtnPageRight_Click(object sender, EventArgs e)
    {
        if ((_currentPage + 1) * PageSize < _projects.Count)
        {
            _currentPage++;
            RefreshList();
            _ = EnrichCurrentPageAsync();
        }
    }

    private void Load_State(object sender, LoadingState state, LoadingState oldState)
    {
    }

    private async void StartSearch() => await LoadProjects(PanSearchBox.Text);

    private async void ResetFilter()
    {
        PanSearchBox.Text = "";
        ComboSearchTag.SelectedIndex = 0;
        ComboSearchSort.SelectedIndex = 0;
        await LoadProjects(null);
    }
}
