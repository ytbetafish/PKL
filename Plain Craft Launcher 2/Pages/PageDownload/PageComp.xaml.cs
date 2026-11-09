using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;
using PCL.Core.UI.Hints;

namespace PCL;

[ContentProperty("SearchTags")]
public partial class PageComp : Grid
{
    private readonly PageCompViewModel _vm = new();

    private List<ModItem> _mods = new();
    private int _currentPage = 0;
    private const int PageSize = 40;

    public PageComp()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += PageComp_Loaded;
    }

    private void PageComp_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载模拟数据
        LoadMockData();
    }

    private void LoadMockData()
    {
        _mods = new List<ModItem>
        {
            new() { Name = "Kerbal Engineer Redux", Author = "cy4n", Description = "在VAB和飞行中显示详细的轨道信息", Downloads = 123456 },
            new() { Name = "MechJeb 2", Author = "arcturus2", Description = "自动化飞行辅助系统", Downloads = 234567 },
            new() { Name = "Deadly Reentry", Author = "Ferram4", Description = "更真实的再入大气层物理效果", Downloads = 98765 },
            new() { Name = "SCANsat", Author = "DMagic", Description = "行星表面扫描和地图系统", Downloads = 87654 },
            new() { Name = "Kerbal Inventory System", Author = " TriggerAu", Description = "宇航员背包和物品系统", Downloads = 76543 },
        };
        RefreshList();
    }

    private void RefreshList()
    {
        PanProjects.Children.Clear();
        var start = _currentPage * PageSize;
        var end = Math.Min(start + PageSize, _mods.Count);
        for (int i = start; i < end; i++)
        {
            PanProjects.Children.Add(CreateModCard(_mods[i]));
        }
        _vm.ApplyPagerState(_currentPage, _mods.Count, PageSize);
    }

    private MyCompItem CreateModCard(ModItem mod)
    {
        var card = new MyCompItem
        {
            Title = mod.Name,
            SubTitle = mod.Author,
            Description = mod.Description,
            Downloads = mod.Downloads.ToString("N0"),
            Tag = mod,
            Margin = new Thickness(0, 0, 0, 8),
        };
        card.Click += (s, e) =>
        {
            Launcher.frmMain.PageChange(new FormMain.PageStackData { page = FormMain.PageType.CompDetail }, parameter: mod);
        };
        return card;
    }

    public ItemCollection SearchTags => ComboSearchTag.Items;

    public string TypeName => "模组";
    public string TypeNameSpaced => "模组";

    public object PageType { get; set; }

    private void PanSearchBox_Search(object sender, EventArgs e)
    {
        StartSearch();
    }

    private void EnterTrigger(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            StartSearch();
    }

    private void BtnSearchReset_Click(object sender, EventArgs e)
    {
        ResetFilter();
    }

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
        _currentPage++;
        RefreshList();
    }

    private void Load_State(object sender, LoadingState state, LoadingState oldState)
    {
    }

    private void StartSearch()
    {
        var keyword = PanSearchBox.Text;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            LoadMockData();
        }
        else
        {
            _mods = _mods.Where(m => m.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                      m.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            _currentPage = 0;
            RefreshList();
        }
    }

    private void ResetFilter()
    {
        PanSearchBox.Text = "";
        ComboSearchTag.SelectedIndex = 0;
        ComboSearchSort.SelectedIndex = 0;
        LoadMockData();
    }
}

public class ModItem
{
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public int Downloads { get; set; }
}





