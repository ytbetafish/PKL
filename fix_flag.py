p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''public partial class PageSettings
{
    public PageSettings()
    {
        InitializeComponent();'''

new = '''public partial class PageSettings
{
    private bool _isInitialized = false;

    public PageSettings()
    {
        InitializeComponent();'''

assert old in t
t = t.replace(old, new, 1)

old2 = '''        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;
        NavKsp.Checked = true;
    }'''

new2 = '''        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;
        _isInitialized = true;
        NavKsp.Checked = true;
    }'''

assert old2 in t
t = t.replace(old2, new2, 1)

old3 = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        LeftNav.TriggerHideAnimation();'''

new3 = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;

        if (!_isInitialized) return;

        LeftNav.TriggerHideAnimation();'''

assert old3 in t
t = t.replace(old3, new3, 1)

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
