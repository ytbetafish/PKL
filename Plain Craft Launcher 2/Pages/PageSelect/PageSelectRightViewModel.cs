using System.Windows;
using PCL.Core.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PCL;

/// <summary>
/// KSP 实例选择页视图模型（对应原版 PageSelectRightViewModel）。
/// 保留原版空态 / 搜索框可见性属性与绑定结构；搜索匹配由原版 McInstance 改为实例路径字符串。
/// </summary>
public partial class PageSelectRightViewModel : ViewModelBase
{
    [ObservableProperty] private Visibility _backVisibility = Visibility.Visible;

    [ObservableProperty] private string _emptyContent = "点击下方按钮选择 KSP 安装目录，将其添加到实例列表。";

    [ObservableProperty] private Visibility _emptyDownloadVisibility = Visibility.Collapsed;

    [ObservableProperty] private Visibility _emptyPanelVisibility = Visibility.Visible;

    [ObservableProperty] private string _emptySearchContent = "没有找到与搜索内容匹配的 KSP 实例目录。";

    [ObservableProperty] private Visibility _emptySearchPanelVisibility = Visibility.Collapsed;

    [ObservableProperty] private string _emptySearchTitle = "没有匹配的实例";

    [ObservableProperty] private string _emptyTitle = "还没有添加 KSP 实例";

    [ObservableProperty] private Visibility _searchBoxVisibility = Visibility.Visible;

    public static bool MatchesSearch(string path, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return true;
        return path.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
