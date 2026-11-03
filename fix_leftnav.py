p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

old = '''        <StackPanel Grid.Column="0" VerticalAlignment="Top">
            <local:MyRadioButton x:Name="NavKsp" GroupName="settingsNav" Text="KSP 实例" Margin="0,0,0,2" />
            <local:MyRadioButton x:Name="NavCkan" GroupName="settingsNav" Text="CKAN 模组" Margin="0,0,0,2" />
            <local:MyRadioButton x:Name="NavTranslate" GroupName="settingsNav" Text="翻译服务" Margin="0,0,0,2" />
            <local:MyRadioButton x:Name="NavAbout" GroupName="settingsNav" Text="关于" Margin="0,0,0,2" />
        </StackPanel>'''

new = '''        <StackPanel Grid.Column="0" VerticalAlignment="Top" Margin="0,12,0,0">
            <TextBlock Text="设置" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
            <local:MyListItem x:Name="NavKsp" Type="RadioBox" Checked="True" IsScaleAnimationEnabled="False"
                              Height="36" MinPaddingRight="35" Title="KSP 实例" SvgIcon="lucide/gamepad-2" />
            <local:MyListItem x:Name="NavCkan" Type="RadioBox" IsScaleAnimationEnabled="False"
                              Height="36" MinPaddingRight="35" Title="CKAN 模组" SvgIcon="lucide/package" />
            <local:MyListItem x:Name="NavTranslate" Type="RadioBox" IsScaleAnimationEnabled="False"
                              Height="36" MinPaddingRight="35" Title="翻译服务" SvgIcon="lucide/languages" />
            <local:MyListItem x:Name="NavAbout" Type="RadioBox" IsScaleAnimationEnabled="False"
                              Height="36" MinPaddingRight="35" Title="关于" SvgIcon="lucide/info" />
        </StackPanel>'''

assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('xaml ok')

# code-behind: wire Check events
p2 = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t2 = open(p2, encoding='utf-8-sig').read()
old2 = '''        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;
        NavKsp.Checked = true;'''
new2 = '''        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;'''
assert old2 in t2, 'old2 not found'
t2 = t2.replace(old2, new2, 1)

# Nav_Changed: use (object sender, RoutedEventArgs e) since MyListItem uses standard Checked
old3 = '''    private void Nav_Changed(MyRadioButton sender, bool raiseByMouse)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
new3 = '''    private void Nav_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        PanKsp.Visibility = NavKsp.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanCkan.Visibility = NavCkan.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanTranslate.Visibility = NavTranslate.Checked ? Visibility.Visible : Visibility.Collapsed;
        PanAbout.Visibility = NavAbout.Checked ? Visibility.Visible : Visibility.Collapsed;
    }'''
assert old3 in t2, 'old3 not found'
t2 = t2.replace(old3, new3, 1)
open(p2, 'w', encoding='utf-8-sig').write(t2)
print('cs ok')
