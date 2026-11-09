# PKL — KSP Launcher

基于 **PCL-CE**（Plain Craft Launcher Community Edition，dev 分支）改造的 **《坎巴拉太空计划》(Kerbal Space Program, KSP)** 启动器。

- 以 PCL-CE 的 WPF 程序骨架为底座，剥离全部 Minecraft 专属内容；
- 集成 **CKAN**（Comprehensive Kerbal Archive Network）核心，实现 KSP 模组的浏览 / 搜索 / 安装 / 依赖解析；
- 参考 PCL-CE 的"简介翻译"，实现模组简介的中 / 英翻译切换。

技术栈：C# / WPF / .NET 10 (`net10.0-windows`)。

---

## 一、上游来源

| 组件 | 仓库 | 版本/分支 | 许可 |
|---|---|---|---|
| 改造底座 | `PCL-Community/PCL-CE` | `dev`（commit `e8d2883`） | Apache-2.0（"Plain Craft Launcher 2"目录除外，沿用其原许可） |
| 模组管理核心 | `KSP-CKAN/CKAN` | `master`，对标 `v1.36.4` | MIT |
| 简介翻译原型 | 取自 PCL-CE `PCL.Launcher.Core/Comp/ProjectProps.cs` 的翻译代理 | — | 同 PCL-CE |

> 最初考虑的 `Hex-Dragon/PCL2 v2.13.1.1` 为 VB.NET + .NET Framework 4.8 旧式工程，本机无对应工具链，故按决策改用已 C# 化、架构更清晰的 PCL-CE dev。

## 二、目录结构

```
PKL/
├─ PCL.Core/                     # 保留：核心工具库（JSON/日志/网络/扩展）
├─ PCL.Core.Wpf/                 # 保留：WPF 主题、控件、动画、图标
├─ PCL.Core.SourceGenerators/    # 保留：源生成器
├─ PCL.Launcher.Core/            # 业务核心（已剥离 MC，新增翻译服务）
│   └─ Translation/              #   ITranslationService / OpenAiTranslationService / TranslationOptions
├─ Plain Craft Launcher 2/       # WPF 主程序
│   ├─ Program.cs / Application   # 启动
│   ├─ FormMain.xaml(.cs)         # 主窗口外壳与导航（三页面：启动/下载/设置）
│   ├─ Pages/PageHome/            #   主页：选择 KSP 路径、显示版本、启动按钮
│   ├─ Pages/PageDownload/        #   下载页：CKAN 模组浏览/搜索/安装
│   ├─ Pages/PageSettings/        #   设置页（左栏导航 + 独立右栏页面结构）
│   │   ├─ PageSettingsLeft.xaml   #   左栏导航（继承 MyPageLeft）
│   │   ├─ PageKspInstance.xaml    #   KSP 实例设置（独立页面）
│   │   ├─ PageCkan.xaml           #   CKAN 模组设置（独立页面）
│   │   ├─ PageTranslate.xaml      #   翻译服务设置（独立页面）
│   │   └─ PageAbout.xaml          #   软件信息（独立页面）
│   └─ Modules/Base/Common/PklSettings.cs   # KSP 路径 + 翻译设置 JSON 持久化
├─ external/CKAN.Core/            # 内联的 CKAN 核心（从 CKAN/Core 复制，目标 net10.0）
├─ src/Pkl.Ckan/                  # CKAN 适配层（无头，不依赖 WPF）
│   ├─ IModService.cs             #   模组管理对外接口
│   ├─ ModService.cs              #   搜索/已安装/安装计划/执行计划
│   ├─ KspInstanceService.cs        #   KSP 实例探测与加载
│   └─ Contracts.cs               #   KspInstanceInfo / ModSearchResult / ModInstallPlan
└─ docs/
    ├─ 改造与整合方案.md
    └─ 参考样例分析.md
```

## 三、构建方式

需要 **.NET 10 SDK**（本机装于 `C:\Users\adminstartor\.dotnet`）。干净构建：

```powershell
# 指向 .NET 10 SDK（若已在 PATH 可省略前两行）
$env:DOTNET_ROOT='C:\Users\adminstartor\.dotnet'
$env:Path='C:\Users\adminstartor\.dotnet;'+$env:Path
# 可选：把 NuGet 缓存与 CLI 主目录重定向到仓库内，避免权限问题
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'

cd PKL
dotnet build ".\Plain Craft Launcher 2.slnx" -c Release
```

预期结果：**0 error**（当前约 464 条 warning，多为可空性与未使用字段，不影响运行）。
运行主程序：`Plain Craft Launcher 2\bin\Release\net10.0-windows\PKL.exe`。

## 四、功能清单

- [x] PCL WPF 外壳：主窗口、主题资源、设置 JSON 持久化、日志、下载引擎框架保留可运行；
- [x] MC 专属内容已剥离：版本隔离、MC 账号/皮肤、Java 检测、Forge/Fabric 安装器、MC Mod 平台、资源包/光影/存档/服务器等页面与模块；
- [x] 三页面导航：启动 / 下载 / 设置，顶栏切换，左栏滑入动画（PageChangeAnim）；
- [x] 设置页结构：左栏导航 + 独立右栏页面（完全照搬原版 PCL 结构），四个独立设置页（KSP 实例 / CKAN 模组 / 翻译服务 / 软件信息）；
- [x] 设置页动画：左栏切换平滑过渡，右栏内容从上到下滑入（完全照搬原版 MyPageRight.TriggerEnterAnimation）；
- [x] 设置页状态保持：左栏只创建一次，切换页面后再回来右栏保持上次选中的栏目；
- [x] CKAN 集成：KSP 路径探测、注册表刷新、模组搜索、已安装列表、依赖解析安装计划、执行安装；
- [x] 简介翻译：模组简介"中/EN"切换，OpenAI 兼容接口（默认 DeepSeek），可在设置页配置 BaseUrl/ApiKey/Model，内存缓存；
- [x] 设置页滚动：内容超出时自动显示垂直滚动条；
- [x] 已去掉：启动时弹出的"PCL CE 版本说明"对话框（ShowCeAnnounce）；
- [x] 已去掉：DEBUG 版本提示对话框（`#if DEBUG || DEBUGCI` 改为 `#if false`）；
- [ ] 启动 KSP 进程（按钮已留占位）；
- [ ] 安装执行进度独立 UI、翻译"测试连接"按钮。

## 五、分层架构

```
┌─────────────────────────────────────────────────────────┐
│  Plain Craft Launcher 2 (WPF, net10.0-windows)          │
│   顶栏导航：启动页 / 下载页 / 设置页                       │
│   设置页：左栏导航（PageSettingsLeft）                    │
│           右栏独立页面（PageKspInstance/PageCkan/...）    │
│   PageHome ─┐                                            │
│   PageDownload┼─> IModService (Pkl.Ckan) ─> CKAN.Core    │
│   PageSettings└─> ITranslationService (OpenAI 兼容)      │
└──────────────┬───────────────────────────┬───────────────┘
               │                           │
         src/Pkl.Ckan (无头适配)      PCL.Core / PCL.Core.Wpf
         external/CKAN.Core           (主题/控件/日志/设置)
         (模组解析/依赖)
```

## 六、设置页动画说明

设置页动画完全照搬原版 PCL 的实现：

- **左栏切换动画**：`PageChangeAnim` 方法
  - 旧左栏 `TriggerHideAnimation()` → 延迟 110ms 换 PanMainLeft.Child → 新左栏 `Opacity=0` → 延迟 30ms `TriggerShowAnimation()`
- **右栏内容切换动画**：`MyPageRight.TriggerEnterAnimation`
  - 逐个元素从上面滑入（TranslateY -16 → 0，每个元素延迟 25ms）
- **左栏只创建一次**：用静态变量 `settingsLeft` 保存，避免每次切换都重新创建
- **右栏状态保持**：用静态变量 `settingsNav` 保存当前选中的栏目，再进入设置页时右栏保持上次选中的栏目

## 七、许可

本仓库为二次开发作品：PCL 外壳部分遵循 PCL-CE 相应许可；CKAN 核心遵循 MIT；新增 PKL 代码可按 Apache-2.0 使用。详见各上游 `LICENSE`。
