# Nulmesis

[English README](./README.md)

Nulmesis 是一个仅面向 Windows 的桌面与命令行工具，用来发现并删除那些通过普通路径语义难以处理的保留名 `nul` 文件。

## 它能做什么

- 无参数启动时打开 WPF 图形界面
- 带参数启动时作为 CLI 运行
- GUI 与 CLI 共用同一套扫描与删除核心逻辑
- 支持两种匹配模式：
  - `strict`：基本名大小写不敏感地等于 `nul`，且文件大小为 `0`
  - `loose`：基本名大小写不敏感地等于 `nul`，忽略文件大小
- 只把基本名精确为 `nul` 的文件视为命中项
- 不会把 `nul.txt`、`nul.log`、`nul.backup` 等扩展名形式当成命中项
- 不会跟随 reparse point

## 项目状态

Nulmesis 当前仍处于 pre-`1.0.0` 阶段。

- 发布线：`0.x`
- 计划中的首个正式版本：`0.1.0`
- 正式发布产物由 CI 在带 tag 的发布流程中生成
- 正式发布文件名会包含系统、架构与版本信息
- 本地手动 publish 产物仅作为验证产物，不视为正式发布资产

## 仓库结构

```text
src/
  Nulmesis.Core/   共享领域模型、匹配规则、扫描器、删除器
  Nulmesis.App/    WPF 外壳、CLI 入口、对话框、ViewModel
tests/
  Nulmesis.Core.Tests/
  Nulmesis.App.Tests/
  Nulmesis.IntegrationTests/
```

## 环境要求

- Windows
- 本地构建与测试需要 .NET 8 SDK

## 构建与测试

```powershell
dotnet test .\Nulmesis.slnx -c Release
```

## 从源码运行

GUI：

```powershell
dotnet run --project .\src\Nulmesis.App
```

CLI 示例：

```powershell
dotnet run --project .\src\Nulmesis.App -- scan --root C:\path\to\target --json
dotnet run --project .\src\Nulmesis.App -- list --root C:\path\to\target
dotnet run --project .\src\Nulmesis.App -- delete --root C:\path\to\target
```

## 安全边界

- 删除范围只限于工具识别出的 delete targets
- 当前有意只处理 `nul` 这一种保留名
- 测试应使用隔离的临时目录，而不是实际工作目录

## 给编码代理的说明

如果你是自动化编码代理，请先阅读 [AGENTS.md](./AGENTS.md)。
