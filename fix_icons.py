p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.Replace('SvgIcon="lucide/gamepad-2"', 'SvgIcon="lucide/rocket"')
t = t.Replace('SvgIcon="lucide/languages"', 'SvgIcon="lucide/globe"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
