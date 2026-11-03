p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('        <Border Grid.Column="0" Background="Transparent">\n        <local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"',
              '        <local:MyScrollViewer Grid.Column="0" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
