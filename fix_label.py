p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
old = '<TextBlock Text="主仓库 URL" FontSize="12" Opacity="0.7" Margin="0,0,0,5" />'
new = '<TextBlock Text="模组镜像源 URL（留空使用 CKAN 官方源）" FontSize="12" Opacity="0.7" Margin="0,0,0,5" />'
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
