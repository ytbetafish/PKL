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
        _vm.ApplyErrorState(null);
        try
        {
            var query = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            var results = await Launcher.Ckan.SearchAsync(query);
            _projects = CkanCompAdapter.ToProjects(results);
            _currentPage = 0;
            RefreshList();
        }
        catch (Exception ex)
        {
            Common.Log(ex, "CKAN 模组列表加载失败");
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
    }

    private void BtnPageLeft_Click(object sender, EventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshList();
        }
    }

    private void BtnPageRight_Click(object sender, EventArgs e)
    {
        if ((_currentPage + 1) * PageSize < _projects.Count)
        {
            _currentPage++;
            RefreshList();
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
