p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        // Find the MyPageLeft parent and trigger hide/show animation like FormMain does
        if (this.FindName("NavKsp") is MyListItem navItem)
        {
            var pageLeft = GetParentOfType<MyPageLeft>(navItem);
            if (pageLeft != null)
                pageLeft.TriggerHideAnimation();
        }

        PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;

        if (this.FindName("NavKsp") is MyListItem navItem2)
        {
            var pageLeft = GetParentOfType<MyPageLeft>(navItem2);
            if (pageLeft != null)
                pageLeft.TriggerShowAnimation();
        }
    }'''

new = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
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
    }'''

assert old in t
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
