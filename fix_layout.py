p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

old = '''            <local:MyCard x:Name="PanKsp" Title="KSP 实例">
                <StackPanel Margin="25,38,23,18">
                    <TextBlock Text="已添加的 KSP 实例" FontSize="12" Opacity="0.7" Margin="0,0,0,5" />
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,14">
                        <ComboBox x:Name="CboInstances" Width="360" Height="32" />
                        <local:MyButton Text="添加实例" Width="96" Margin="10,0,0,0" Click="BtnAddInstance_Click" />
                    </StackPanel>
                    <TextBlock Text="切换实例后需要重启 PKL 生效。" FontSize="11" Opacity="0.5" />
                </StackPanel>
            </local:MyCard>'''

new = '''            <local:MyCard x:Name="PanKsp" Title="KSP 实例">
                <StackPanel Margin="25,38,23,18">
                    <Grid Margin="0,0,0,10">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="130" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="已添加的 KSP 实例" FontSize="12" Opacity="0.7" VerticalAlignment="Center" />
                        <StackPanel Grid.Column="1" Orientation="Horizontal">
                            <ComboBox x:Name="CboInstances" Width="360" Height="32" />
                            <local:MyButton Text="添加实例" Width="96" Margin="10,0,0,0" Click="BtnAddInstance_Click" />
                        </StackPanel>
                    </Grid>
                    <Grid Margin="0,0,0,10">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="130" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="当前实例路径" FontSize="12" Opacity="0.7" VerticalAlignment="Center" />
                        <TextBlock x:Name="LabKspPath" Grid.Column="1" Text="" FontSize="12" Opacity="0.9"
                                   VerticalAlignment="Center" TextTrimming="CharacterEllipsis" />
                    </Grid>
                    <Grid Margin="0,0,0,10">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="130" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="切换实例后" FontSize="12" Opacity="0.7" VerticalAlignment="Center" />
                        <TextBlock Grid.Column="1" Text="需要重启 PKL 生效" FontSize="12" Opacity="0.5" VerticalAlignment="Center" />
                    </Grid>
                </StackPanel>
            </local:MyCard>'''

assert old in t, 'ksp old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ksp ok')
