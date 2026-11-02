# PKL — Plain KSP Launcher

基于 PCL（Plain Craft Launcher）骨架改造的《坎巴拉太空计划》（Kerbal Space Program）专用启动器，内置完整 CKAN 模组管理功能。

## 项目背景

- **底座**：基于 [PCL 2.13.1.1](https://github.com/Hex-Dragon/PCL2) 的 WPF 程序骨架
- **模组管理**：完整集成 [KSP-CKAN](https://github.com/KSP-CKAN/CKAN) 核心功能
- **翻译功能**：参考 [PCL-CE](https://github.com/PCL-Community/PCL-CE) 的简介翻译实现

## 核心功能

1. **KSP 游戏启动** — 基于 PCL 的窗口框架、主题系统、设置系统
2. **CKAN 模组管理** — 模组搜索、安装、更新、依赖解析、兼容性检查
3. **简介翻译** — 模组描述自动翻译，支持原文/译文切换
4. **下载引擎** — 高速多线程下载，国内镜像加速
5. **日志系统** — 完整的运行日志与崩溃诊断

## 技术栈

- **框架**：WPF (.NET 8 / .NET 9)
- **语言**：C#
- **平台**：Windows 10 1809+
- **许可证**：见各上游仓库原始许可证

## 项目结构

```
PKL/
├── PKL.Launcher/          # 主启动器项目（WPF）
├── PKL.Core/              # 核心类库（配置、下载、日志）
├── PKL.CKAN/              # CKAN 集成层
├── docs/                  # 项目文档
└── README.md
```

## 编译方式

1. 安装 .NET 9 SDK
2. 还原 NuGet 包：`dotnet restore`
3. 编译：`dotnet build`

## 上游致谢

- [Hex-Dragon/PCL2](https://github.com/Hex-Dragon/PCL2) — 启动器骨架
- [PCL-Community/PCL-CE](https://github.com/PCL-Community/PCL-CE) — 功能参考
- [KSP-CKAN/CKAN](https://github.com/KSP-CKAN/CKAN) — 模组管理核心
注：本项目正在开发
