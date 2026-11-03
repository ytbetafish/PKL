import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml"
s = io.open(f, encoding="utf-8-sig").read()
s = s.replace('Text="主页"', 'Text="启动"')
s = s.replace('Text="KSP 模组"', 'Text="下载"')
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
