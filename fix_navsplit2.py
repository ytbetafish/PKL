import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()
n = s.count("new Pages.PageHomeLeft()")
s = s.replace("new Pages.PageHomeLeft()", "new PageLaunchLeft()")
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print(f"replaced {n}")
