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
| 改造底座 | `PCL-Community/PCL-CE` | `dev`（commit `74c4e2d`） | Apache-2.0（"Plain Craft Launcher 2"目录除外，沿用其原许可） |
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
│   ├─ FormMain.xaml(.cs)         # 主窗口外壳与导航
│   ├─ Pages/PageHome/            #   主页：选择 KSP 路径、显示版本、启动按钮（占位）
│   ├─ Pages/PageMods/            #   模组管理：搜索/列表/安装计划/简介翻译切换
│   ├─ Pages/PageSettings/        #   设置：翻译 API（BaseUrl/Key/Model）持久化
│   └─ Modules/Base/Common/PklSettings.cs   # KSP 路径 + 翻译设置 JSON 持久化
├─ external/CKAN.Core/            # 内联的 CKAN 核心（从 CKAN/Core 复制，目标 net10.0）
├─ src/Pkl.Ckan/                  # CKAN 适配层（无头，不依赖 WPF）
│   ├─ IModService.cs             #   模组管理对外接口
│   ├─ ModService.cs              #   搜索/已安装/安装计划/执行计划
│   ├─ KspInstanceService.cs      #   KSP 实例探测与加载
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
dotnet build ".\Plain Craft Launcher 2.slnx" -c Debug
```

预期结果：**0 error**（当前约 394 条 warning，多为可空性与未使用字段，不影响运行）。
运行主程序：`Plain Craft Launcher 2\bin\Debug\net10.0-windows\Plain Craft Launcher 2.exe`。

## 四、功能清单

- [x] PCL WPF 外壳：主窗口、主题资源、设置 JSON 持久化、日志、下载引擎框架保留可运行；
- [x] MC 专属内容已剥离：版本隔离、MC 账号/皮肤、Java 检测、Forge/Fabric 安装器、MC Mod 平台、资源包/光影/存档/服务器等页面与模块；
- [x] CKAN 集成：KSP 路径探测、注册表刷新、模组搜索、已安装列表、依赖解析安装计划、执行安装；
- [x] 简介翻译：模组简介"中/EN"切换，OpenAI 兼容接口（默认 DeepSeek），可在设置页配置 BaseUrl/ApiKey/Model，内存缓存；
- [ ] 启动 KSP 进程（按钮已留占位）；
- [ ] 安装执行进度独立 UI、翻译"测试连接"按钮。

## 五、分层架构

```
┌─────────────────────────────────────────────────────────┐
│  Plain Craft Launcher 2 (WPF, net10.0-windows)           │
│   PageHome ─┐                                            │
│   PageMods ─┼─> IModService (Pkl.Ckan) ─> CKAN.Core      │
│   PageSettings└─> ITranslationService (OpenAI 兼容)      │
└──────────────┬───────────────────────────┬───────────────┘
                │                            │
         src/Pkl.Ckan (无头适配)      PCL.Core / PCL.Core.Wpf
         external/CKAN.Core           (主题/控件/日志/设置)
         (模组解析/依赖)
```

## 六、许可

本仓库为二次开发作品：PCL 外壳部分遵循 PCL-CE 相应许可；CKAN 核心遵循 MIT；新增 PKL 代码可按 Apache-2.0 使用。详见各上游 `LICENSE`。
