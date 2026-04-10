# Nulmesis

[English README](./README.md)

Nulmesis 是一个仅面向 Windows 的 Rust 工具集，用来发现并删除那些基本名精确等于 `nul`、且常规方式难以处理的阻塞文件。

## 它能做什么

- 通过 `nulmesis-cli` 作为命令行工具运行
- 通过 `nulmesis-desktop` 作为 Tauri 桌面应用运行
- GUI 与 CLI 共用同一套 Rust core
- 只匹配基本名精确为 `nul` 的文件
- 不会把 `nul.txt`、`nul.log`、`nul.backup` 等类似名称当成命中项
- 不跟随 reparse point
- 支持两种扫描模式：
  - `strict`：精确 `nul` 且文件大小为 0 字节
  - `loose`：精确 `nul`，不限制文件大小

## 仓库结构

```text
crates/
  nulmesis-core/   共享领域模型、扫描/删除服务、路径处理
  nulmesis-cli/    命令行入口与 JSON/文本输出
apps/
  desktop/         Tauri 桌面壳、前端与打包配置
```

## 环境要求

- Windows
- Rust toolchain
- Node.js 20+

## 构建与测试

```powershell
cargo test
```

桌面前端构建：

```powershell
npm install --prefix .\apps\desktop
npm run frontend:build --prefix .\apps\desktop
```

桌面 dirty 包：

```powershell
npx tauri build --no-bundle --config .\apps\desktop\src-tauri\tauri.conf.json
```

## 从源码运行

CLI 示例：

```powershell
cargo run -p nulmesis-cli -- scan --root C:\path\to\target --json
cargo run -p nulmesis-cli -- list --root C:\path\to\target --mode loose
cargo run -p nulmesis-cli -- delete --root C:\path\to\target --mode loose
```

桌面开发模式：

```powershell
npm run dev --prefix .\apps\desktop
```

## 发布策略

- 以 CI 在带 tag 流程中产出的资产为准
- 本地构建仅作为 dirty 验证产物
- 发布文件名必须包含产品形态、平台、架构与版本

## 安全边界

- 当前只处理 `nul` 这一种保留名
- 删除范围只限于精确命中的 `nul` 文件目标
- 会跳过 reparse point
- GUI 对高风险根目录应要求用户明确确认后再扫描

## 给编码代理的说明

如果你是自动化编码代理，请先阅读 [AGENTS.md](./AGENTS.md)。
