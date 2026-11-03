p = r'Plain Craft Launcher 2\FormMain.xaml.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''        // PLC 彩蛋
        if (RandomUtils.NextInt(1, 1000) == 233)'''
new = '''        // PKL 品牌：显示 PKL 文字，隐藏 CE 角标
        LabTitleLogo.Text = "PKL";
        LabTitleLogo.Visibility = Visibility.Visible;
        CELogo.Visibility = Visibility.Collapsed;
        // PLC 彩蛋
        if (RandomUtils.NextInt(1, 1000) == 233)'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
