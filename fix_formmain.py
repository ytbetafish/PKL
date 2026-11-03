import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()

repls = [
    ("PanMainRight.Child = new MyPageRight { Child = new Pages.PageHome() };",
     "PanMainRight.Child = new Pages.PageHome();"),
    ("UserControl page = tag switch",
     "MyPageRight page = tag switch"),
    ("PanMainRight.Child = new MyPageRight { Child = page };",
     "PanMainRight.Child = page;"),
]
for old, new in repls:
    assert s.count(old) == 1, f"count={s.count(old)} for {old!r}"
    s = s.replace(old, new)

io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
