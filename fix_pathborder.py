p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
old = 'Background="{DynamicResource ColorBrushTransparentBackground}" CornerRadius="4" Padding="12,4">'
new = 'Background="{DynamicResource ColorBrushCardBackground}" BorderBrush="{DynamicResource ColorBrushHalfWhite}" BorderThickness="1" CornerRadius="4" Padding="12,8">'
t = t.replace(old, new)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
