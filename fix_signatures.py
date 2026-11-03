import io, re

base = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\Pages"

# (file, [method names wired to MyButton.Click that need MouseButtonEventArgs])
targets = {
    base + r"\PageHome\PageHome.xaml.cs": ["BtnPickPath_Click", "BtnLaunch_Click"],
    base + r"\PageMods\PageMods.xaml.cs": ["BtnInstalled_Click", "BtnTranslate_Click", "BtnInstall_Click"],
    base + r"\PageSettings\PageSettings.xaml.cs": ["BtnSave_Click"],
}

for f, methods in targets.items():
    s = io.open(f, encoding="utf-8-sig").read()
    for m in methods:
        old = f"private async void {m}(object sender, RoutedEventArgs e)"
        if old in s:
            s = s.replace(old, f"private async void {m}(object sender, System.Windows.Input.MouseButtonEventArgs e)")
            print(f"  {f}: {m} (async)")
            continue
        old2 = f"private void {m}(object sender, RoutedEventArgs e)"
        if old2 in s:
            s = s.replace(old2, f"private void {m}(object sender, System.Windows.Input.MouseButtonEventArgs e)")
            print(f"  {f}: {m}")
            continue
        print(f"  {f}: {m} NOT FOUND (check manually)")
    io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("done")
