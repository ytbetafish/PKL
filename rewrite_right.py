p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

# Find and replace the entire right column Grid content
import re
# Match from <Grid Grid.Column="1" to </Grid> before closing </Grid>
# Simpler: replace PanKsp, PanCkan, PanTranslate, PanAbout blocks

old_ksp = '''            <local:MyCard x:Name="PanKsp" Title="KSP 实例">
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

new_ksp = '''            <local:MyCard x:Name="PanKsp" Title="KSP 实例">
                <StackPanel Margin="25,30,25,20">
                    <Grid Margin="0,0,0,12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="已添加的 KSP 实例" VerticalAlignment="Center" FontSize="13"/>
                        <Grid Grid.Column="1">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <ComboBox x:Name="CboInstances" Height="36"/>
                            <local:MyButton Grid.Column="1" Text="添加实例" Margin="10,0,0,0" Height="36" Width="100" Click="BtnAddInstance_Click"/>
                        </Grid>
                    </Grid>
                    <Grid Margin="0,0,0,12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="当前实例路径" VerticalAlignment="Center" FontSize="13"/>
                        <Border Grid.Column="1" Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,9">
                            <TextBlock x:Name="LabKspPath" Text="" FontSize="12" TextTrimming="CharacterEllipsis"/>
                        </Border>
                    </Grid>
                    <Grid Margin="0,0,0,12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="KSP 版本" VerticalAlignment="Center" FontSize="13"/>
                        <TextBlock Grid.Column="1" x:Name="LabKspVersion" Text="" FontSize="13"/>
                    </Grid>
                </StackPanel>
            </local:MyCard>'''

assert old_ksp in t, 'ksp old not found'
t = t.replace(old_ksp, new_ksp, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
