p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

# Fix duplicated attributes
t = t.replace(
    '<local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"\n                              Style="{StaticResource ScrollViewerFullMargin}"> HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"\n                              Style="{StaticResource ScrollViewerFullMargin}">',
    '<local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"\n                              Style="{StaticResource ScrollViewerFullMargin}">')

# Width 200 -> 160
t = t.replace('<ColumnDefinition Width="200" />', '<ColumnDefinition Width="160" />')

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
