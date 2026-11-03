import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()

# 1) Constructor: line 137 — replace direct right-child with split
old_ctor = "        PanMainRight.Child = new Pages.PageHome();"
new_ctor = (
"        PanMainLeft.Child = new Pages.PageHomeLeft();\n"
"        PanMainRight.Child = new Pages.PageHome();"
)
assert s.count(old_ctor) == 1, f"ctor count={s.count(old_ctor)}"
s = s.replace(old_ctor, new_ctor)

# 2) Nav handler: rewrite to set left/right based on tag
old_nav = (
"        MyPageRight page = tag switch\n"
"        {\n"
"            \"1\" => new Pages.PageMods(),\n"
"            \"2\" => new Pages.PageSettings(),\n"
"            \"3\" => new Pages.PageHome(),\n"
"            _ => new Pages.PageHome(),\n"
"        };\n"
"        PanMainRight.Child = page;\n"
"    }"
)
new_nav = (
"        switch (tag)\n"
"        {\n"
"            case \"1\":\n"
"                PanMainLeft.Child = pageLeft;\n"
"                PanMainRight.Child = new Pages.PageMods();\n"
"                break;\n"
"            case \"2\":\n"
"                PanMainLeft.Child = pageLeft;\n"
"                PanMainRight.Child = new Pages.PageSettings();\n"
"                break;\n"
"            default:\n"
"                PanMainLeft.Child = new Pages.PageHomeLeft();\n"
"                PanMainRight.Child = new Pages.PageHome();\n"
"                break;\n"
"        }\n"
"    }\n"
"\n"
"    public void SelectNavTag(string tag)\n"
"    {\n"
"        var btn = tag switch\n"
"        {\n"
"            \"1\" => BtnTitleSelect1,\n"
"            \"2\" => BtnTitleSelect2,\n"
"            _ => BtnTitleSelect0,\n"
"        };\n"
"        if (btn is not null) btn.IsChecked = true;\n"
"    }"
)
assert s.count(old_nav) == 1, f"nav count={s.count(old_nav)}"
s = s.replace(old_nav, new_nav)

io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
