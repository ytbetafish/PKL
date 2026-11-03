import io
edits = [
    (r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\Pages\PageMods\PageMods.xaml.cs",
     "public partial class PageMods : UserControl", "public partial class PageMods"),
    (r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs",
     "public partial class PageSettings : UserControl", "public partial class PageSettings"),
]
for f, old, new in edits:
    s = io.open(f, encoding="utf-8-sig").read()
    assert s.count(old) == 1, f"{f}: count={s.count(old)}"
    s = s.replace(old, new)
    io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
    print("ok", f)
