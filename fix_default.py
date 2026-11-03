p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

old = '''            svc.MirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\\r','\\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            svc.RepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "MirrorOnly";
            await svc.RefreshMasterRegistryAsync();'''
new = '''            svc.MirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\\r','\\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            svc.OfficialRepoUrl = PklSettings.Current.CkanMasterRepoUrl;
            svc.RepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "OfficialFirst";
            await svc.RefreshMasterRegistryAsync();'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
