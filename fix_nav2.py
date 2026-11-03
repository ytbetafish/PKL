p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
print('before', t.count('Checked="Nav_Changed"'))
t = t.replace('Checked="Nav_Changed"', 'Check="Nav_Changed"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('after', t.count('Check="Nav_Changed"'))
