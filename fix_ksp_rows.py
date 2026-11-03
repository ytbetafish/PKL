p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

# Add more rows to PanKsp: exe path, GameData, logs
old = '''                        <Grid.RowDefinitions>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                        </Grid.RowDefinitions>'''

new = '''                        <Grid.RowDefinitions>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="34"/>
                        </Grid.RowDefinitions>'''

assert old in t, 'rowdefs not found'
t = t.replace(old, new, 1)

# Add new rows after KSP version row
old2 = '''                        <TextBlock Grid.Row="4" VerticalAlignment="Center" Margin="0,0,25,0" Text="KSP 版本"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" x:Name="LabKspVersion" Text="" VerticalAlignment="Center" FontSize="13"/>
                    </Grid>'''

new2 = '''                        <TextBlock Grid.Row="4" VerticalAlignment="Center" Margin="0,0,25,0" Text="KSP 版本"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" x:Name="LabKspVersion" Text="" VerticalAlignment="Center" FontSize="13"/>
                        <TextBlock Grid.Row="6" VerticalAlignment="Center" Margin="0,0,25,0" Text="游戏可执行文件"/>
                        <Border Grid.Row="6" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">
                            <TextBlock x:Name="LabKspExe" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Grid.Row="8" VerticalAlignment="Center" Margin="0,0,25,0" Text="数据目录 (GameData)"/>
                        <Border Grid.Row="8" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">
                            <TextBlock x:Name="LabKspGameData" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Grid.Row="10" VerticalAlignment="Center" Margin="0,0,25,0" Text="日志目录"/>
                        <Border Grid.Row="10" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">
                            <TextBlock x:Name="LabKspLogs" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                        </Border>
                    </Grid>'''

assert old2 in t, 'ksp version row not found'
t = t.replace(old2, new2, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('xaml ok')
