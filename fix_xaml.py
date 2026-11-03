p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml'
t = open(p, encoding='utf-8-sig').read()
# Remove event wiring from XAML entirely; wire in code-behind instead.
t = t.replace(' Check="Nav_Changed"', '')
open(p, 'w', encoding='utf-8-sig').write(t)
print('xaml ok')

p2 = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t2 = open(p2, encoding='utf-8-sig').read()
# Add event wiring in ctor
old = '''        RefreshInstancesList();
    }'''
new = '''        RefreshInstancesList();
        NavKsp.Check += Nav_Changed;
        NavCkan.Check += Nav_Changed;
        NavTranslate.Check += Nav_Changed;
        NavAbout.Check += Nav_Changed;
        NavKsp.Checked = true;
    }'''
assert old in t2, 'ctor end not found'
t2 = t2.replace(old, new, 1)
open(p2, 'w', encoding='utf-8-sig').write(t2)
print('cs ok')
