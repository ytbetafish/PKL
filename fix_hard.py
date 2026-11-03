p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace(
    '<Border Grid.Column="0" Background="{DynamicResource ColorBrushCardBackground}">',
    '<Border Grid.Column="0" Background="#2D2D30">')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
