import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\Pages\PageMods\PageMods.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()
old = (
"    private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await DoSearch(TxtSearch.Text);\r\n"
"    private async void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)\r\n"
"    {\r\n"
"        if (e.Key == System.Windows.Input.Key.Enter)\r\n"
"            await DoSearch(TxtSearch.Text);\r\n"
"    }"
)
new = "    private async void BtnSearch_Click(object sender, EventArgs e) => await DoSearch(TxtSearch.Text);"
if old not in s:
    old = old.replace("\r\n", "\n"); new = new.replace("\r\n", "\n")
assert s.count(old) == 1, f"count={s.count(old)}"
s = s.replace(old, new)
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
