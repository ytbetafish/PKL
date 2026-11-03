p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('IsScaleAnimationEnabled="False"', 'IsScaleAnimationEnabled="True"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
