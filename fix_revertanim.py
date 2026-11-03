p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('IsScaleAnimationEnabled="True"', 'IsScaleAnimationEnabled="False"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
