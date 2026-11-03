p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace(
    '        <local:MyScrollViewer Grid.Column="0" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"',
    '        <Border Grid.Column="0" Background="{DynamicResource ColorBrushCardBackground}">\n        <local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"')
t = t.replace(
    '        </local:MyScrollViewer>\n\n        <Grid Grid.Column="1"',
    '        </local:MyScrollViewer>\n        </Border>\n\n        <Grid Grid.Column="1"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
