p = r'PCL.Launcher.Core\Updates\UpdateManager.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''    private static void LoadOnlineInfo()
    {
        ScheduleBasedOnConfig();
        AnnouncementService.Load();
    }'''
new = '''    private static void LoadOnlineInfo()
    {
        // PKL: 禁用 PCL-CE 自动更新检查与公告拉取
        return;
    }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
