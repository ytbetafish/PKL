using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace PCL;

[ContentProperty("SearchTags")]
public partial class PageComp : Grid
{
    private readonly PageCompViewModel _vm = new();

    public PageComp()
    {
        InitializeComponent();
        DataContext = _vm;
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
        // TODO: 切换到第一页
    }

    private void BtnPageLeft_Click(object sender, EventArgs e)
    {
        // TODO: 切换到上一页
    }

    private void BtnPageRight_Click(object sender, EventArgs e)
    {
        // TODO: 切换到下一页
    }

    private void Load_State(object sender, LoadingState state, LoadingState oldState)
    {
        // TODO: 加载状态变化
    }

    private void StartSearch()
    {
        // TODO: 从网络下载模组列表
        _vm.ApplyErrorState("暂未实现，敬请期待");
        _vm.IsErrorVisible = true;
    }

    private void ResetFilter()
    {
        PanSearchBox.Text = "";
        ComboSearchTag.SelectedIndex = 0;
        ComboSearchSort.SelectedIndex = 0;
    }
}
