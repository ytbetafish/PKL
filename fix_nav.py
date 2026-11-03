import io, sys

f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()

# Edit 1: wrap startup PageHome in MyPageRight
old1 = "        PanMainRight.Child = new Pages.PageHome();"
new1 = "        PanMainRight.Child = new MyPageRight { Child = new Pages.PageHome() };"
assert s.count(old1) == 1, f"old1 count={s.count(old1)}"
s = s.replace(old1, new1)

# Edit 2: route all KSP nav tags to custom pages wrapped in MyPageRight
old2 = (
    '        // KSP 挂接：用 MyPageRight 包装 UserControl，避免原生代码强转失败\r\n'
    '        if (tag == "1") { PanMainRight.Child = new MyPageRight { Child = new Pages.PageMods() }; return; }\r\n'
    '        if (tag == "3") { PanMainRight.Child = new MyPageRight { Child = new Pages.PageHome() }; return; }\r\n'
    '        var pageType = (PageType)int.Parse(tag ?? "0");\r\n'
    '        PageChangeActual(pageType, PageSubType.Default);'
)
new2 = (
    '        // KSP 挂接：全部导航直接托管自定义页面并用 MyPageRight 包装，\r\n'
    '        // 绕过原生 PageChangeActual，避免对 PanMainRight.Child 做 MyPageRight 强转时 InvalidCastException。\r\n'
    '        UserControl page = tag switch\r\n'
    '        {\r\n'
    '            "1" => new Pages.PageMods(),\r\n'
    '            "2" => new Pages.PageSettings(),\r\n'
    '            "3" => new Pages.PageHome(),\r\n'
    '            _ => new Pages.PageHome(),\r\n'
    '        };\r\n'
    '        PanMainRight.Child = new MyPageRight { Child = page };'
)
if old2 not in s:
    old2 = old2.replace("\r\n", "\n")
    new2 = new2.replace("\r\n", "\n")
assert s.count(old2) == 1, f"old2 count={s.count(old2)}"
s = s.replace(old2, new2)

io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("OK: edits applied")
