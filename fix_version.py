p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''        try
        {
            var versionTxt = Path.Combine(root, "version.txt");
            LabKspVersion.Text = File.Exists(versionTxt) ? File.ReadAllText(versionTxt).Trim() : "未知";
        }
        catch { LabKspVersion.Text = "读取失败"; }'''

new = '''        try
        {
            var buildId = Path.Combine(root, "KSP_Data", "buildID.txt");
            var versionTxt = Path.Combine(root, "version.txt");
            if (File.Exists(buildId))
                LabKspVersion.Text = File.ReadAllText(buildId).Trim();
            else if (File.Exists(versionTxt))
                LabKspVersion.Text = File.ReadAllText(versionTxt).Trim();
            else
                LabKspVersion.Text = "未知";
        }
        catch { LabKspVersion.Text = "读取失败"; }'''

assert old in t
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
