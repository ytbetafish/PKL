p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.Replace('Checked="Nav_Changed"', 'Check="Nav_Changed"')
open(p, 'w', encoding='utf-8-sig').write(t)

p2 = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t2 = open(p2, encoding='utf-8-sig').read()
old = '''    private void Nav_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
new = '''    private void Nav_Changed(MyRadioButton sender, bool raiseByMouse)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
assert old in t2, 'old not found'
t2 = t2.replace(old, new, 1)
open(p2, 'w', encoding='utf-8-sig').write(t2)
print('ok')
