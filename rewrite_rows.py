p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

# Replace PanKsp
old_ksp_start = t.index('<local:MyCard x:Name="PanKsp"')
old_ksp_end = t.index('</local:MyCard>', old_ksp_start) + len('</local:MyCard>')

new_ksp = '''<local:MyCard x:Name="PanKsp" Title="KSP 实例">
                <StackPanel Margin="25,40,25,21">
                    <Grid Grid.IsSharedSizeScope="True">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" SharedSizeGroup="Name"/>
                            <ColumnDefinition Width="1*"/>
                            <ColumnDefinition Width="60"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" VerticalAlignment="Center" Margin="0,0,25,0" Text="已添加的 KSP 实例"/>
                        <Grid Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="2">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <ComboBox x:Name="CboInstances" Height="28"/>
                            <local:MyButton Grid.Column="1" Text="添加实例" Margin="10,0,0,0" Height="28" Width="100" Click="BtnAddInstance_Click"/>
                        </Grid>
                        <TextBlock Grid.Row="2" VerticalAlignment="Center" Margin="0,0,25,0" Text="当前实例路径"/>
                        <Border Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">
                            <TextBlock x:Name="LabKspPath" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Grid.Row="4" VerticalAlignment="Center" Margin="0,0,25,0" Text="KSP 版本"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" x:Name="LabKspVersion" Text="" VerticalAlignment="Center" FontSize="13"/>
                    </Grid>
                </StackPanel>
            </local:MyCard>'''

t = t[:old_ksp_start] + new_ksp + t[old_ksp_end:]

# Replace PanCkan
old_ckan_start = t.index('<local:MyCard x:Name="PanCkan"')
old_ckan_end = t.index('</local:MyCard>', old_ckan_start) + len('</local:MyCard>')

new_ckan = '''<local:MyCard x:Name="PanCkan" Title="CKAN 模组源" Visibility="Collapsed">
                <StackPanel Margin="25,40,25,21">
                    <Grid Grid.IsSharedSizeScope="True">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" SharedSizeGroup="Name"/>
                            <ColumnDefinition Width="1*"/>
                            <ColumnDefinition Width="60"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="60"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" VerticalAlignment="Center" Margin="0,0,25,0" Text="数据目录"/>
                        <Grid Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="2">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <Border Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">
                                <TextBlock x:Name="LabCkanDataDir" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                            </Border>
                            <local:MyButton Grid.Column="1" Text="选择目录" Margin="10,0,0,0" Height="28" Width="100" Click="BtnPickCkanDir_Click"/>
                        </Grid>
                        <TextBlock Grid.Row="2" VerticalAlignment="Top" Margin="0,0,25,0" Text="镜像源 URL"/>
                        <TextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtMirrorUrls" AcceptsReturn="True"
                                 TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"
                                 Background="{DynamicResource ColorBrushHalfWhite}" BorderBrush="{DynamicResource ColorBrushBg0}"
                                 BorderThickness="1" Padding="6,4" FontSize="12"/>
                        <TextBlock Grid.Row="4" VerticalAlignment="Center" Margin="0,0,25,0" Text="下载源策略"/>
                        <ComboBox Grid.Row="4" Grid.Column="1" Grid.ColumnSpan="2" x:Name="CboStrategy" Height="28">
                            <ComboBoxItem Content="镜像源为主，官方源补缺失（推荐）" Tag="OfficialFirst"/>
                            <ComboBoxItem Content="仅使用镜像源" Tag="MirrorOnly"/>
                            <ComboBoxItem Content="仅使用官方源" Tag="OfficialOnly"/>
                        </ComboBox>
                        <TextBlock Grid.Row="6" VerticalAlignment="Center" Margin="0,0,25,0" Text="稳定性容忍度"/>
                        <StackPanel Grid.Row="6" Grid.Column="1" Grid.ColumnSpan="2" Orientation="Horizontal" VerticalAlignment="Center">
                            <local:MyRadioButton x:Name="OptStable" GroupName="stability" Text="Stable"/>
                            <local:MyRadioButton x:Name="OptTesting" GroupName="stability" Text="Testing" Margin="16,0,0,0"/>
                            <local:MyRadioButton x:Name="OptPre" GroupName="stability" Text="PreRelease" Margin="16,0,0,0"/>
                        </StackPanel>
                        <TextBlock Grid.Row="8" VerticalAlignment="Center" Margin="0,0,25,0" Text="HTTP 代理"/>
                        <local:MyTextBox Grid.Row="8" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtProxy" Height="28"/>
                        <TextBlock Grid.Row="10" VerticalAlignment="Center" Margin="0,0,25,0" Text="并发下载数"/>
                        <local:MyTextBox Grid.Row="10" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtConcurrency" Height="28"/>
                        <TextBlock Grid.Row="12" VerticalAlignment="Center" Margin="0,0,25,0" Text="超时秒数"/>
                        <local:MyTextBox Grid.Row="12" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtTimeout" Height="28"/>
                        <TextBlock Grid.Row="14" VerticalAlignment="Center" Margin="0,0,25,0" Text="模组索引"/>
                        <StackPanel Grid.Row="14" Grid.Column="1" Grid.ColumnSpan="2" Orientation="Horizontal" VerticalAlignment="Center">
                            <local:MyButton x:Name="BtnRefreshRepo" Text="刷新索引" Height="28" Width="100" Click="BtnRefreshRepo_Click"/>
                            <TextBlock x:Name="LabCkanStatus" Text="" FontSize="12" Opacity="0.7" Margin="14,0,0,0" VerticalAlignment="Center"/>
                        </StackPanel>
                        <TextBlock Grid.Row="16" VerticalAlignment="Center" Margin="0,0,25,0" Text="清单管理"/>
                        <StackPanel Grid.Row="16" Grid.Column="1" Grid.ColumnSpan="2" Orientation="Horizontal" VerticalAlignment="Center">
                            <local:MyButton Text="导出清单" Width="110" Height="28" Click="BtnExportMods_Click"/>
                            <local:MyButton Text="导入 .ckan" Width="110" Height="28" Margin="10,0,0,0" Click="BtnImportMods_Click"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </local:MyCard>'''

t = t[:old_ckan_start] + new_ckan + t[old_ckan_end:]

# Replace PanTranslate
old_tr_start = t.index('<local:MyCard x:Name="PanTranslate"')
old_tr_end = t.index('</local:MyCard>', old_tr_start) + len('</local:MyCard>')

new_tr = '''<local:MyCard x:Name="PanTranslate" Title="翻译服务" Visibility="Collapsed">
                <StackPanel Margin="25,40,25,21">
                    <Grid Grid.IsSharedSizeScope="True">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" SharedSizeGroup="Name"/>
                            <ColumnDefinition Width="1*"/>
                            <ColumnDefinition Width="60"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                            <RowDefinition Height="9"/>
                            <RowDefinition Height="28"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" VerticalAlignment="Center" Margin="0,0,25,0" Text="接口 Base URL"/>
                        <local:MyTextBox Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtBaseUrl" Height="28"/>
                        <TextBlock Grid.Row="2" VerticalAlignment="Center" Margin="0,0,25,0" Text="API Key"/>
                        <local:MyTextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtApiKey" Height="28"/>
                        <TextBlock Grid.Row="4" VerticalAlignment="Center" Margin="0,0,25,0" Text="模型"/>
                        <local:MyTextBox Grid.Row="4" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtModel" Height="28"/>
                        <StackPanel Grid.Row="6" Grid.Column="1" Grid.ColumnSpan="2" Orientation="Horizontal" VerticalAlignment="Center">
                            <local:MyButton x:Name="BtnSave" Text="保存设置" Width="110" Height="28" ColorType="Highlight" Click="BtnSave_Click"/>
                            <TextBlock x:Name="LabSaved" Text="" FontSize="12" Opacity="0.7" Margin="14,0,0,0" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </local:MyCard>'''

t = t[:old_tr_start] + new_tr + t[old_tr_end:]

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
