using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace PCL;

[ContentProperty("SearchTags")]
public partial class PageComp : Grid
{
    public PageComp()
    {
        InitializeComponent();
    }

    public ItemCollection SearchTags => ComboSearchTag.Items;

    public string TypeName => "模组";
    public string TypeNameSpaced => "模组";

    public object PageType { get; set; }

    private void PanSearchBox_Search(object sender, EventArgs e) { }
    private void EnterTrigger(object sender, KeyEventArgs e) { }
    private void BtnSearchReset_Click(object sender, EventArgs e) { }
    private void BtnPageFirst_Click(object sender, EventArgs e) { }
    private void BtnPageLeft_Click(object sender, EventArgs e) { }
    private void BtnPageRight_Click(object sender, EventArgs e) { }
    private void Load_State(object sender, LoadingState state, LoadingState oldState) { }
}
