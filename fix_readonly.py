p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

replacements = [
    ('''<Border Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushCardBackground}" BorderBrush="#3B82F6" BorderThickness="1" CornerRadius="4" Padding="12,8">
                            <TextBlock x:Name="LabKspPath" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                        </Border>''',
     '''<local:MyTextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="2" x:Name="LabKspPath" Height="34" IsReadOnly="True" />'''),
    ('''<Border Grid.Row="6" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushCardBackground}" BorderBrush="#3B82F6" BorderThickness="1" CornerRadius="4" Padding="12,8">
                            <TextBlock x:Name="LabKspExe" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                        </Border>''',
     '''<local:MyTextBox Grid.Row="6" Grid.Column="1" Grid.ColumnSpan="2" x:Name="LabKspExe" Height="34" IsReadOnly="True" />'''),
    ('''<Border Grid.Row="8" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushCardBackground}" BorderBrush="#3B82F6" BorderThickness="1" CornerRadius="4" Padding="12,8">
                            <TextBlock x:Name="LabKspGameData" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                        </Border>''',
     '''<local:MyTextBox Grid.Row="8" Grid.Column="1" Grid.ColumnSpan="2" x:Name="LabKspGameData" Height="34" IsReadOnly="True" />'''),
    ('''<Border Grid.Row="10" Grid.Column="1" Grid.ColumnSpan="2" Background="{DynamicResource ColorBrushCardBackground}" BorderBrush="#3B82F6" BorderThickness="1" CornerRadius="4" Padding="12,8">
                            <TextBlock x:Name="LabKspLogs" Text="" FontSize="12" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                        </Border>''',
     '''<local:MyTextBox Grid.Row="10" Grid.Column="1" Grid.ColumnSpan="2" x:Name="LabKspLogs" Height="34" IsReadOnly="True" />'''),
]

for old, new in replacements:
    assert old in t, f'not found: {old[:50]}'
    t = t.replace(old, new, 1)

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
