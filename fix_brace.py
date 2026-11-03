p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''        };
    }

    }

    private void BtnAddInstance_Click'''
new = '''        };
    }

    private void BtnAddInstance_Click'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
