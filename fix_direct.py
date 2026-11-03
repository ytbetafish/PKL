p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace(
    '<local:MyPageLeft Grid.Column="0" Background="#2D2D30">',
    '<local:MyPageLeft x:Name="LeftNav" Grid.Column="0" Background="#2D2D30">')
open(p, 'w', encoding='utf-8-sig').write(t)

p2 = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t2 = open(p2, encoding='utf-8-sig').read()

old = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        MyPageLeft? pageLeft = null;
        if (this.FindName("NavKsp") is MyListItem navItem)
            pageLeft = GetParentOfType<MyPageLeft>(navItem);

        pageLeft?.TriggerHideAnimation();

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
            PCL.Animations.AaCode(() => pageLeft?.TriggerShowAnimation(), 30, true)
        }, "PageSettings NavSwitch");
    }

    private static T? GetParentOfType<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null && parent is not T)
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        return parent as T;
    }'''

new = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
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

assert old in t2
t2 = t2.replace(old, new, 1)
open(p2, 'w', encoding='utf-8-sig').write(t2)
print('ok')
