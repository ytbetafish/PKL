p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
    {
        PanKsp.Visibility = sender == NavKsp ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = sender == NavCkan ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = sender == NavTranslate ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = sender == NavAbout ? Visibility.Visible : Visibility.Collapsed;

        System.Windows.FrameworkElement? shown =
            sender == NavKsp ? PanKsp :
            sender == NavCkan ? PanCkan :
            sender == NavTranslate ? PanTranslate :
            sender == NavAbout ? PanAbout : null;
        if (shown != null)
        {
            shown.Opacity = 0;
            PCL.Animations.AniStart(new[]
            {
                PCL.Animations.AaCode(() => shown.Opacity = 1, 200)
            }, "PageSettings NavFade");
        }
    }'''

new = '''    private void Nav_Changed(object sender, PCL.Common.RouteEventArgs e)
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
    }

    private static T? GetParentOfType<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null && parent is not T)
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        return parent as T;
    }'''

assert old in t
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
