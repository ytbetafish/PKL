using CommunityToolkit.Mvvm.ComponentModel;
using PCL.Core.ViewModel;

namespace PCL;

/// <summary>资源下载页组件卡片的视图模型。</summary>
public partial class PageCompViewModel : ViewModelBase
{
    [ObservableProperty] private string _pageText = "1";

    [ObservableProperty] private bool _canGoFirst;

    [ObservableProperty] private double _firstOpacity = 0.2d;

    [ObservableProperty] private bool _canGoLeft;

    [ObservableProperty] private double _leftOpacity = 0.2d;

    [ObservableProperty] private bool _canGoRight = true;

    [ObservableProperty] private double _rightOpacity = 1d;

    [ObservableProperty] private bool _isPagerVisible = false;

    [ObservableProperty] private bool _isErrorVisible;

    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty] private string _loadingText = "正在获取模组列表";

    public void ApplyPagerState(int currentPage, int totalItems, int pageSize)
    {
        PageText = (currentPage + 1).ToString();
        IsPagerVisible = totalItems > pageSize;
        CanGoFirst = currentPage > 1;
        FirstOpacity = currentPage > 1 ? 1d : 0.2d;
        CanGoLeft = currentPage > 0;
        LeftOpacity = currentPage > 0 ? 1d : 0.2d;
        CanGoRight = totalItems > pageSize * (currentPage + 1);
        RightOpacity = CanGoRight ? 1d : 0.2d;
    }

    public void ApplyErrorState(string? errorMessage)
    {
        IsErrorVisible = errorMessage is not null;
        ErrorMessage = errorMessage ?? string.Empty;
    }
}
