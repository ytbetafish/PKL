p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
t = open(p, encoding='utf-8-sig').read()

# ctor: replace TxtCkanRepoUrl init with TxtMirrorUrls + strategy
old1 = '        TxtCkanRepoUrl.Text = PklSettings.Current.CkanMasterRepoUrl;'
new1 = '''        TxtMirrorUrls.Text = string.Join("\\n", PklSettings.Current.CkanMirrorUrls);
        foreach (System.Windows.Controls.ComboBoxItem item in CboStrategy.Items)
        {
            if ((string)item.Tag == PklSettings.Current.CkanRepoStrategy) { CboStrategy.SelectedItem = item; break; }
        }
        if (CboStrategy.SelectedItem == null) CboStrategy.SelectedIndex = 0;'''
assert old1 in t, 'ctor line not found'
t = t.replace(old1, new1, 1)

# Refresh button: svc.MirrorUrl → svc.MirrorUrls + RepoStrategy
old2 = '            svc.MirrorUrl = TxtCkanRepoUrl.Text.Trim();'
new2 = '''            svc.MirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\\r','\\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            svc.RepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "MirrorOnly";'''
assert old2 in t, 'refresh line not found'
t = t.replace(old2, new2, 1)

# Test repo button: TxtCkanRepoUrl → first mirror URL
old3 = '        var url = TxtCkanRepoUrl.Text.Trim();'
new3 = '''        var url = TxtMirrorUrls.Text.Split(new[]{'\\r','\\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).FirstOrDefault();
        if (string.IsNullOrEmpty(url)) { LabCkanStatus.Text = "未填镜像 URL。"; return; }'''
assert old3 in t, 'test line not found'
t = t.replace(old3, new3, 1)

# BtnSave: write mirror urls + strategy back
old4 = '        PklSettings.Current.CkanMasterRepoUrl = TxtCkanRepoUrl.Text.Trim();'
new4 = '''        PklSettings.Current.CkanMirrorUrls = TxtMirrorUrls.Text.Split(new[]{'\\r','\\n'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        PklSettings.Current.CkanRepoStrategy = (CboStrategy.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "MirrorOnly";'''
assert old4 in t, 'save line not found'
t = t.replace(old4, new4, 1)

open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
