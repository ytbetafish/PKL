import io
f = r"C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\Plain Craft Launcher 2\FormMain.xaml.cs"
s = io.open(f, encoding="utf-8-sig").read()
old = "        PanMainRight.Child = new Pages.PageHome();"
new = (
"        try {\n"
"            PanMainRight.Child = new Pages.PageHome();\n"
"        } catch (Exception _dbgEx) {\n"
"            System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, \"pkl_debug.txt\"), _dbgEx.ToString());\n"
"            throw;\n"
"        }"
)
assert s.count(old) == 1, f"count={s.count(old)}"
s = s.replace(old, new)
io.open(f, "w", encoding="utf-8-sig", newline="").write(s)
print("ok")
