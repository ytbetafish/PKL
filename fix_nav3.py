import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml"
s = io.open(f, encoding="utf-8-sig").read()
old = '<local:MyRadioButton x:Name="BtnTitleSelect3" Visibility="Visible"\n                                                 Text="KSP 启动"'
new = '<local:MyRadioButton x:Name="BtnTitleSelect3" Visibility="Collapsed"\n                                                 Text="KSP 启动"'
assert s.count(old) == 1, f"crlf count={s.count(old)}"
s = s.replace(old, new)
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
