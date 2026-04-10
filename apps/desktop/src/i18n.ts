export type Language = "en" | "zh";

const STORAGE_KEY = "nulmesis-language";

const messages = {
  en: {
    titlebarLabel: "nul reserved-name cleanup",
    langToggle: "中文",
    themeLight: "Light",
    themeDark: "Dark",
    infoToggle: "Info",
    infoHide: "Hide",
    subtitleInfo: "Local dirty build overview.",
    root: "Root",
    mode: "Mode",
    browse: "📂 Browse",
    scan: "🔍 Scan",
    stop: "⏹️ Stop",
    stopping: "⏳ Stopping…",
    rescan: "🔄 Rescan",
    deleteSelected: "🗑️ Delete selected",
    selectAll: "☑️ Select all",
    clearSelection: "🧹 Clear",
    scanStatus: "Scan status",
    matches: "Matches",
    noScanYet: "No scan yet",
    noMatches: "No exact `nul` matches in the current result.",
    rawJson: "Show raw JSON",
    strictOption: "Strict mode",
    looseOption: "Loose mode",
    strictHint: "Strict mode finds only zero-byte exact `nul` files.",
    looseHint: "Loose mode also catches non-zero `nul` files that still behave like blocked reserved names.",
    chooseRoot: "Choose a root directory before scanning.",
    scanningRoot: "Scanning {root}…",
    scanCancelled: "Scan cancelled.",
    stoppingScan: "Stopping scan…",
    deletingSelected: "Deleting {count} selected target(s)…",
    guardedAuto: "High-risk root detected ({root}). Automatic scan was skipped.",
    guardedPanelTitle: "High-risk scan",
    guardedPanelBody: "{root} is a drive root or top-level share. This can take a long time even though the UI stays responsive.",
    guardedContinue: "⚠️ Scan anyway",
    guardedBack: "↩️ Back",
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
    statusShell: "Desktop shell ready.",
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
    infoPendingProject: "https://github.com/Coahuilite/Nulmesis",
    heroTitleTail: "a revenge to nul",
    subtitlePrefix: "nul corrodes in silence, ",
    subtitleMiddle: " and ",
    subtitleTail: " will guard your glory.",
  },
  zh: {
    titlebarLabel: "nul 保留名清理工具",
    langToggle: "EN",
    themeLight: "浅色",
    themeDark: "深色",
    infoToggle: "信息",
    infoHide: "收起",
    subtitleInfo: "本地 dirty 构建概览。",
    root: "根目录",
    mode: "模式",
    browse: "📂 浏览",
    scan: "🔍 扫描",
    stop: "⏹️ 停止",
    stopping: "⏳ 正在停止…",
    rescan: "🔄 重新扫描",
    deleteSelected: "🗑️ 删除选中",
    selectAll: "☑️ 全选可删项",
    clearSelection: "🧹 清空",
    scanStatus: "扫描状态",
    matches: "命中结果",
    noScanYet: "尚未扫描",
    noMatches: "当前结果中没有精确 `nul` 命中。",
    rawJson: "显示原始 JSON",
    strictOption: "严格模式",
    looseOption: "宽松模式",
    strictHint: "严格模式只会命中 0 字节的精确 `nul` 文件。",
    looseHint: "宽松模式会额外包含非 0KB、但仍表现为受阻保留名的 `nul` 文件。",
    chooseRoot: "请先选择根目录再扫描。",
    scanningRoot: "正在扫描 {root}…",
    scanCancelled: "扫描已取消。",
    stoppingScan: "正在停止扫描…",
    deletingSelected: "正在删除 {count} 个选中目标…",
    guardedAuto: "检测到高风险根目录（{root}），已跳过自动扫描。",
    guardedPanelTitle: "高风险扫描",
    guardedPanelBody: "{root} 属于盘符根目录或顶层共享，扫描时间可能很长，但界面会保持响应。",
    guardedContinue: "⚠️ 继续扫描",
    guardedBack: "↩️ 返回",
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
    infoPendingProject: "https://github.com/Coahuilite/Nulmesis",
    heroTitleTail: "是一场对 nul 的复仇",
    subtitlePrefix: "nul在悄无声息的腐蚀，",
    subtitleMiddle: " 与 ",
    subtitleTail: " 会守护你的荣光。",
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
