p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('System.Windows.Controls.Control? shown', 'System.Windows.FrameworkElement? shown')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
