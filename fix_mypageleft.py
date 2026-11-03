p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

old = '''        <Border Grid.Column="0" Background="#2D2D30">
        <local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"
                              Style="{StaticResource ScrollViewerFullMargin}">
            <StackPanel Margin="0,12,0,0">
                <TextBlock Text="KSP" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavKsp" IsScaleAnimationEnabled="False" Checked="True" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="实例管理" SvgIcon="lucide/rocket" />

                <TextBlock Text="模组" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavCkan" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="CKAN 模组源" SvgIcon="lucide/package" />
                <local:MyListItem x:Name="NavTranslate" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="翻译服务" SvgIcon="lucide/globe" />

                <TextBlock Text="关于" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavAbout" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="软件信息" SvgIcon="lucide/info" />
            </StackPanel>
        </local:MyScrollViewer>
        </Border>'''

new = '''        <local:MyPageLeft Grid.Column="0">
            <StackPanel Margin="0,12,0,0">
                <TextBlock Text="KSP" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavKsp" IsScaleAnimationEnabled="False" Checked="True" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="实例管理" SvgIcon="lucide/rocket" />

                <TextBlock Text="模组" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavCkan" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="CKAN 模组源" SvgIcon="lucide/package" />
                <local:MyListItem x:Name="NavTranslate" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="翻译服务" SvgIcon="lucide/globe" />

                <TextBlock Text="关于" Margin="13,6,5,4" Opacity="0.6" FontSize="12" />
                <local:MyListItem x:Name="NavAbout" IsScaleAnimationEnabled="False" Type="RadioBox"
                                  MinPaddingRight="35" Height="36" VerticalAlignment="Top"
                                  Title="软件信息" SvgIcon="lucide/info" />
            </StackPanel>
        </local:MyPageLeft>'''

assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
