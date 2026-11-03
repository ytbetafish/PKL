p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;

        if (!_isInitialized) return;

        LeftNav.TriggerHideAnimation();

        PCL.Animations.AniStop("PageSettings NavSwitch");
        PCL.Animations.AniStart(new[]
        {
            PCL.Animations.AaCode(() =>
            {
                PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
                PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
                PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
                PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;
            }, 110),
            PCL.Animations.AaCode(() => LeftNav.TriggerShowAnimation(), 30, true)
        }, "PageSettings NavSwitch");
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
