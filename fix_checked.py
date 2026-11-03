import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()
s = s.replace("btn.IsChecked = true;", "btn.Checked = true;")
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
