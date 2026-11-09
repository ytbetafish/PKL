namespace PCL;

public partial class PageModDetail
{
    public PageModDetail(ModItem mod)
    {
        InitializeComponent();
        LabTitle.Text = mod.Name;
        LabAuthor.Text = $"作者: {mod.Author}";
        LabDescription.Text = mod.Description;
    }
}
