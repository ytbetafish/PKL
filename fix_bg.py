p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()

old = '''    <Grid Margin="25,25,25,25">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="180" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <local:MyScrollViewer Grid.Column="0"'''

new = '''    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <Border Grid.Column="0" Background="{DynamicResource ColorBrushCardBackground}">
        <local:MyScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto"
                              Style="{StaticResource ScrollViewerFullMargin}">'''

assert old in t, 'old not found'
t = t.replace(old, new, 1)

# Close the Border after MyScrollViewer
old2 = '''        </local:MyScrollViewer>

        <Grid Grid.Column="1" Margin="20,0,0,0">'''

new2 = '''        </local:MyScrollViewer>
        </Border>

        <Grid Grid.Column="1" Margin="25,25,25,25">'''

assert old2 in t, 'old2 not found'
t = t.replace(old2, new2, 1)

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
