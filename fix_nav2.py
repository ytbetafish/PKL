p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = Visibility.Collapsed;
        PanCkan.Visibility = Visibility.Collapsed;
        PanTranslate.Visibility = Visibility.Collapsed;
        PanAbout.Visibility = Visibility.Collapsed;
        if (NavKsp.Checked) PanKsp.Visibility = Visibility.Visible;
        else if (NavCkan.Checked) PanCkan.Visibility = Visibility.Visible;
        else if (NavTranslate.Checked) PanTranslate.Visibility = Visibility.Visible;
        else if (NavAbout.Checked) PanAbout.Visibility = Visibility.Visible;
    }'''

new = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;
    }'''

assert old in t
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
