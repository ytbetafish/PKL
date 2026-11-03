p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

old = '''        <StackPanel Grid.Column="0" VerticalAlignment="Top">
            <local:MyRadioButton x:Name="NavKsp" GroupName="settingsNav" Text="KSP 实例" Margin="0,0,0,8" />
            <local:MyRadioButton x:Name="NavCkan" GroupName="settingsNav" Text="CKAN 模组" Margin="0,0,0,8" />
            <local:MyRadioButton x:Name="NavTranslate" GroupName="settingsNav" Text="翻译服务" Margin="0,0,0,8" />
            <local:MyRadioButton x:Name="NavAbout" GroupName="settingsNav" Text="关于" Margin="0,0,0,8" />
        </StackPanel>'''

new = '''        <local:MyScrollViewer Grid.Column="0" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="0,12,0,0">
                <TextBlock Text="设置" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavKsp" Type="RadioBox" Checked="True"
                                  MinPaddingRight="35" Height="36" Title="KSP 实例" SvgIcon="lucide/gamepad-2" />
                <local:MyListItem x:Name="NavCkan" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" Title="CKAN 模组" SvgIcon="lucide/package" />
                <local:MyListItem x:Name="NavTranslate" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" Title="翻译服务" SvgIcon="lucide/languages" />
                <local:MyListItem x:Name="NavAbout" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" Title="关于" SvgIcon="lucide/info" />
            </StackPanel>
        </local:MyScrollViewer>'''

assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('xaml ok')
