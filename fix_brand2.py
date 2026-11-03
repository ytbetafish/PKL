p = r'Plain Craft Launcher 2\FormMain.xaml.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''        // PKL 品牌：显示 PKL 文字，隐藏 CE 角标
        LabTitleLogo.Text = "PKL";
        LabTitleLogo.Visibility = Visibility.Visible;
        CELogo.Visibility = Visibility.Collapsed;'''
new = '''        // PKL 品牌：隐藏原版 PCL 矢量 logo + CE 角标，显示 PKL 文字
        try
        {
            ShapeTitleLogo.Visibility = Visibility.Collapsed;
            ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
            CELogo.Visibility = Visibility.Collapsed;
            LabTitleLogo.Text = "PKL";
            LabTitleLogo.Visibility = Visibility.Visible;
        }
        catch { }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
