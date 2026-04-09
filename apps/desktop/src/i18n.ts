export type Language = "en" | "zh";

const STORAGE_KEY = "nulmesis-language";

const messages = {
  en: {
    titlebarLabel: "nul-only cleanup",
    langToggle: "中文",
    themeLight: "Light",
    themeDark: "Dark",
    infoToggle: "Info",
    infoHide: "Hide",
    eyebrow: "Windows reserved-name cleanup",
    subtitle: "Rust + Tauri dirty shell with a shared scan/delete core.",
    root: "Root",
    mode: "Mode",
    browse: "📂 Browse",
    scan: "🔍 Scan",
    stop: "⏹️ Stop",
    stopping: "⏳ Stopping…",
    rescan: "🔄 Rescan",
    deleteSelected: "🗑️ Delete selected",
    scanStatus: "Scan status",
    matches: "Matches",
    noScanYet: "No scan yet",
    rawJson: "Show raw JSON",
    strictHint: "Strict finds only 0KB `nul`. Switch to loose if the blocked `nul` has non-zero size.",
    looseHint: "Loose also catches non-zero `nul` files that still behave like blocked reserved names.",
    chooseRoot: "Choose a root directory before scanning.",
    scanningRoot: "Scanning {root}…",
    scanCancelled: "Scan cancelled.",
    stoppingScan: "Stopping scan…",
    deletingSelected: "Deleting {count} selected target(s)…",
    deleteCancelled: "Delete cancelled.",
    guardedAuto: "High-risk root detected ({root}). Automatic scan was skipped. Click Scan to continue intentionally.",
    guardedCancel: "High-risk root {root} was not scanned.",
    guardedConfirm: "Scan the high-risk root {root}? This can take a long time even though the UI remains responsive.",
    badgeIdle: "idle",
    badgeReady: "ready",
    badgePartial: "partial",
    badgeFailed: "failed",
    badgeGuarded: "guarded",
    badgeScanning: "scanning",
    badgeStopping: "stopping",
    badgeDeleting: "deleting",
    badgeNeedsRoot: "needs root",
    statusReady: "Ready to scan.",
    statusFailed: "The last action failed.",
    statusShell: "Shell ready.",
    filterAll: "All exact nul",
    filterDeleteTargets: "Delete targets",
    filterReview: "Scan-only matches",
    cardsRoot: "Root",
    cardsMode: "Mode",
    cardsMatches: "Matches",
    cardsDeleteTargets: "Delete targets",
    cardsErrors: "Errors",
    cardsDuration: "Duration",
    cardsDeleted: "Deleted",
    cardsFailedDeletes: "Delete failures",
    bytes: "bytes",
    deleteTarget: "delete target",
    scanOnlyMatch: "scan-only match",
    infoTitle: "About this build",
    infoBuild: "Build",
    infoVersion: "Version",
    infoEngine: "Engine",
    infoPlatform: "Platform",
    infoProject: "Project",
    infoAuthors: "Credits",
    infoPendingProject: "Cloud repository pending",
    infoLogoAlt: "Nulmesis logo",
    enTitleTail: "a revenge to nul",
    zhTitleTail: "是一场对 nul 的复仇",
  },
  zh: {
    titlebarLabel: "保留名清理工具",
    langToggle: "EN",
    themeLight: "浅色",
    themeDark: "深色",
    infoToggle: "信息",
    infoHide: "收起",
    eyebrow: "Windows 保留名清理",
    subtitle: "基于 Rust + Tauri 的 dirty 本地构建，共享同一套 scan/delete core。",
    root: "根目录",
    mode: "模式",
    browse: "📂 浏览",
    scan: "🔍 扫描",
    stop: "⏹️ 停止",
    stopping: "⏳ 正在停止…",
    rescan: "🔄 重新扫描",
    deleteSelected: "🗑️ 删除选中",
    scanStatus: "扫描状态",
    matches: "命中结果",
    noScanYet: "尚未扫描",
    rawJson: "显示原始 JSON",
    strictHint: "Strict 只会命中 0KB 的 `nul`。如果被阻塞的 `nul` 不是空文件，请切到 loose。",
    looseHint: "Loose 会额外包含非 0KB、但依旧表现为受阻保留名的 `nul` 文件。",
    chooseRoot: "请先选择根目录再扫描。",
    scanningRoot: "正在扫描 {root}…",
    scanCancelled: "扫描已取消。",
    stoppingScan: "正在停止扫描…",
    deletingSelected: "正在删除 {count} 个选中目标…",
    deleteCancelled: "删除已取消。",
    guardedAuto: "检测到高风险根目录（{root}）。已跳过自动扫描，请手动点击扫描后再继续。",
    guardedCancel: "已取消对高风险根目录 {root} 的扫描。",
    guardedConfirm: "确定要扫描高风险根目录 {root} 吗？即使界面保持响应，也可能耗时较长。",
    badgeIdle: "空闲",
    badgeReady: "就绪",
    badgePartial: "部分完成",
    badgeFailed: "失败",
    badgeGuarded: "受保护",
    badgeScanning: "扫描中",
    badgeStopping: "停止中",
    badgeDeleting: "删除中",
    badgeNeedsRoot: "缺少根目录",
    statusReady: "可以开始扫描。",
    statusFailed: "上一次操作失败。",
    statusShell: "桌面壳已就绪。",
    filterAll: "全部精确 nul",
    filterDeleteTargets: "可删目标",
    filterReview: "仅扫描命中",
    cardsRoot: "根目录",
    cardsMode: "模式",
    cardsMatches: "命中数",
    cardsDeleteTargets: "可删数",
    cardsErrors: "错误数",
    cardsDuration: "耗时",
    cardsDeleted: "已删除",
    cardsFailedDeletes: "删除失败",
    bytes: "字节",
    deleteTarget: "可删除目标",
    scanOnlyMatch: "仅扫描命中",
    infoTitle: "构建信息",
    infoBuild: "构建渠道",
    infoVersion: "版本",
    infoEngine: "技术栈",
    infoPlatform: "平台",
    infoProject: "项目地址",
    infoAuthors: "作者与致谢",
    infoPendingProject: "云端仓库尚未建立",
    infoLogoAlt: "Nulmesis 标识",
    enTitleTail: "a revenge to nul",
    zhTitleTail: "是一场对 nul 的复仇",
  },
} as const;

export function getInitialLanguage(): Language {
  const saved = localStorage.getItem(STORAGE_KEY);
  if (saved === "en" || saved === "zh") {
    return saved;
  }

  return navigator.language.toLowerCase().startsWith("zh") ? "zh" : "en";
}

export function persistLanguage(language: Language) {
  localStorage.setItem(STORAGE_KEY, language);
  document.documentElement.lang = language;
  document.documentElement.dataset.lang = language;
}

export function t(language: Language, key: keyof typeof messages.en, params?: Record<string, string | number>) {
  let template = messages[language][key] ?? messages.en[key];
  if (!params) {
    return template;
  }

  for (const [name, value] of Object.entries(params)) {
    template = template.replaceAll(`{${name}}`, String(value));
  }

  return template;
}
