p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
old = '''            <local:MyCard Margin="0,0,0,15" Title="KSP 设置">
                <Grid Margin="25,38,23,18">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <Border Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,9">
                        <TextBlock x:Name="LabKspPath" Text="未设置" Opacity="0.8"
                                   FontSize="12.5" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                    </Border>
                    <local:MyButton Grid.Column="1" Text="选择路径" Width="96" Margin="10,0,0,0"
                                    Click="BtnPickKspPath_Click" />
                </Grid>
            </local:MyCard>

'''
new = ''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
