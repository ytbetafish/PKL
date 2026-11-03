p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''        RefreshInstancesList();
        NavKsp.Check += Nav_Changed;'''

new = '''        RefreshInstancesList();
        RefreshKspInfo();
        NavKsp.Check += Nav_Changed;'''

assert old in t
t = t.replace(old, new, 1)

# Add RefreshKspInfo method after RefreshInstancesList
old2 = '''    private void BtnAddInstance_Click'''
new2 = '''    private void RefreshKspInfo()
    {
        var root = PklSettings.Current.KspRootPath;
        LabKspPath.Text = string.IsNullOrEmpty(root) ? "（未设置）" : root;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            LabKspVersion.Text = "—";
            LabKspExe.Text = "—";
            LabKspGameData.Text = "—";
            LabKspLogs.Text = "—";
            return;
        }
        try
        {
            var versionTxt = Path.Combine(root, "version.txt");
            LabKspVersion.Text = File.Exists(versionTxt) ? File.ReadAllText(versionTxt).Trim() : "未知";
        }
        catch { LabKspVersion.Text = "读取失败"; }
        LabKspExe.Text = File.Exists(Path.Combine(root, "KSP_x64.exe")) ? Path.Combine(root, "KSP_x64.exe") :
                         File.Exists(Path.Combine(root, "ksp.exe")) ? Path.Combine(root, "ksp.exe") : "未找到";
        var gd = Path.Combine(root, "GameData");
        LabKspGameData.Text = Directory.Exists(gd) ? gd : "不存在";
        var logs = Path.Combine(root, "KSP_Data", "output_log.txt");
        LabKspLogs.Text = File.Exists(logs) ? logs : Path.Combine(root, "KSP_Data");
    }

    private void BtnAddInstance_Click'''

assert old2 in t
t = t.replace(old2, new2, 1)

# Also call RefreshKspInfo after instance switch
old3 = '''                PklSettings.Current.KspRootPath = s;
                PklSettings.Save();
                LabSaved.Text = "已切换 KSP 实例，重启 PKL 后生效。";'''
new3 = '''                PklSettings.Current.KspRootPath = s;
                PklSettings.Save();
                RefreshKspInfo();
                LabSaved.Text = "已切换 KSP 实例。";'''
assert old3 in t
t = t.replace(old3, new3, 1)

open(p, 'w', encoding='utf-8-sig').write(t)
print('cs ok')
