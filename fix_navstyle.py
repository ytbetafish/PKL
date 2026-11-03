p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.Replace('Margin="0,0,0,8" />', 'Margin="0,0,0,2" />')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
