p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

# 1. ComboBox -> MyComboBox
t = t.replace('<ComboBox ', '<local:MyComboBox ')
t = t.replace('</ComboBox>', '</local:MyComboBox>')

# 2. 多行 TextBox (TxtMirrorUrls) -> MyTextBox
t = t.replace('<TextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtMirrorUrls" AcceptsReturn="True"\n                                 TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"\n                                 Background="{DynamicResource ColorBrushHalfWhite}" BorderBrush="{DynamicResource ColorBrushBg0}"\n                                 BorderThickness="1" Padding="6,4" FontSize="12"/>',
              '<local:MyTextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" x:Name="TxtMirrorUrls" AcceptsReturn="True"\n                                 TextWrapping="Wrap" VerticalScrollBarVisibility="Auto" Height="60"/>')

# 3. Height=28 -> Height=34 (all controls)
t = t.replace('Height="28"', 'Height="34"')

# 4. RowDefinition Height=28 -> 34
t = t.replace('<RowDefinition Height="28"/>', '<RowDefinition Height="34"/>')

# 5. Column 60 -> Auto
t = t.replace('<ColumnDefinition Width="60"/>', '<ColumnDefinition Width="Auto"/>')

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
