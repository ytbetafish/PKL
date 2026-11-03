p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''    private void Nav_Changed(MyRadioButton sender, bool raiseByMouse)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
new = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
