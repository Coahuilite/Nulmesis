import { invoke } from "@tauri-apps/api/core";
import { getCurrentWindow } from "@tauri-apps/api/window";

import { getInitialLanguage, persistLanguage, t, type Language } from "./i18n";

type Match = {
  absolutePath: string;
  relativePath: string;
  fileName: string;
  sizeBytes: number;
  lastWriteTimeUtc: string;
};

type ScanResult = {
  matches: Match[];
  deleteTargets: Match[];
  errors: { kind: string; path: string; message: string }[];
  summary: { matchedCount: number; errorCount: number; durationMs: number; root: string; mode: string };
};

type DeleteResult = {
  summary: { requestedCount: number; deletedCount: number; failedCount: number; cancelled: boolean };
  errors: { path: string; message: string }[];
};

type ShellStatus = {
  appName: string;
  shell: string;
  mode: string;
  scope: string;
  currentDir: string;
  version: string;
  buildChannel: string;
  projectUrl: string;
  engine: string;
  os: string;
  arch: string;
  authors: string[];
};

type Activity = "idle" | "scanning" | "stopping" | "deleting";

const appWindow = getCurrentWindow();

const rootInput = document.querySelector<HTMLInputElement>("#root")!;
const browseButton = document.querySelector<HTMLButtonElement>("#browse")!;
const modeSelect = document.querySelector<HTMLSelectElement>("#mode")!;
const scanButton = document.querySelector<HTMLButtonElement>("#scan")!;
const deleteButton = document.querySelector<HTMLButtonElement>("#delete")!;
const resultList = document.querySelector<HTMLUListElement>("#results")!;
const statusText = document.querySelector<HTMLParagraphElement>("#status-text")!;
const statusRaw = document.querySelector<HTMLPreElement>("#status-raw")!;
const statusBadge = document.querySelector<HTMLSpanElement>("#status-badge")!;
const statusCards = document.querySelector<HTMLDivElement>("#status-cards")!;
const resultsSummary = document.querySelector<HTMLSpanElement>("#results-summary")!;
const themeToggle = document.querySelector<HTMLButtonElement>("#theme-toggle")!;
const langToggle = document.querySelector<HTMLButtonElement>("#lang-toggle")!;
const infoToggle = document.querySelector<HTMLButtonElement>("#info-toggle")!;
const infoDrawer = document.querySelector<HTMLElement>("#info-drawer")!;
const rawDetails = document.querySelector<HTMLDetailsElement>(".raw-status")!;

let latestResult: ScanResult | null = null;
let latestDelete: DeleteResult | null = null;
let latestRawPayload: unknown = {};
let shellStatus: ShellStatus | null = null;
let selected = new Set<string>();
let activity: Activity = "idle";
let hasCompletedScan = false;
let language: Language = getInitialLanguage();

function keyForMode(mode: string) {
  return mode === "loose" ? "looseHint" : "strictHint";
}

function currentBadgeKey() {
  switch (activity) {
    case "scanning":
      return "badgeScanning";
    case "stopping":
      return "badgeStopping";
    case "deleting":
      return "badgeDeleting";
    default:
      return statusBadge.dataset.state === "partial"
        ? "badgePartial"
        : statusBadge.dataset.state === "failed"
          ? "badgeFailed"
          : statusBadge.dataset.state === "guarded"
            ? "badgeGuarded"
            : statusBadge.dataset.state === "needs-root"
              ? "badgeNeedsRoot"
              : statusBadge.dataset.state === "ready"
                ? "badgeReady"
                : "badgeIdle";
  }
}

function setBadge(state: string) {
  statusBadge.dataset.state = state;
  statusBadge.className = `badge badge-${state}`;
  const key = currentBadgeKey() as Parameters<typeof t>[1];
  statusBadge.textContent = t(language, key);
}

function applyTheme(theme: "light" | "dark") {
  document.documentElement.dataset.theme = theme;
  localStorage.setItem("nulmesis-theme", theme);
  const icon = document.querySelector<HTMLSpanElement>("#theme-icon");
  const label = document.querySelector<HTMLSpanElement>("#theme-label");
  if (icon && label) {
    icon.textContent = theme === "dark" ? "🌙" : "☀️";
    label.textContent = t(language, theme === "dark" ? "themeDark" : "themeLight");
  }
}

function initializeTheme() {
  const savedTheme = localStorage.getItem("nulmesis-theme");
  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  applyTheme(savedTheme === "light" || savedTheme === "dark" ? savedTheme : prefersDark ? "dark" : "light");
}

function setLanguage(nextLanguage: Language) {
  language = nextLanguage;
  persistLanguage(language);
  document.querySelector<HTMLElement>("#hero-eyebrow")!.textContent = t(language, "eyebrow");
  document.querySelector<HTMLElement>("#hero-subtitle")!.textContent = t(language, "subtitle");
  document.querySelector<HTMLElement>("#root-label")!.textContent = t(language, "root");
  document.querySelector<HTMLElement>("#mode-label")!.textContent = t(language, "mode");
  document.querySelector<HTMLElement>("#scan-status-label")!.textContent = t(language, "scanStatus");
  document.querySelector<HTMLElement>("#matches-label")!.textContent = t(language, "matches");
  document.querySelector<HTMLElement>("#raw-json-label")!.textContent = t(language, "rawJson");
  document.querySelector<HTMLElement>("#titlebar-label")!.textContent = t(language, "titlebarLabel");
  document.querySelector<HTMLElement>("#info-toggle-label")!.textContent = infoDrawer.classList.contains("hidden") ? t(language, "infoToggle") : t(language, "infoHide");
  document.querySelector<HTMLElement>("#lang-toggle-label")!.textContent = t(language, "langToggle");
  document.querySelector<HTMLElement>("#info-eyebrow")!.textContent = t(language, "eyebrow");
  document.querySelector<HTMLElement>("#info-title")!.textContent = t(language, "infoTitle");
  document.querySelector<HTMLElement>("#info-subtitle")!.textContent = t(language, "subtitle");
  document.querySelector<HTMLElement>("#info-build-label")!.textContent = t(language, "infoBuild");
  document.querySelector<HTMLElement>("#info-version-label")!.textContent = t(language, "infoVersion");
  document.querySelector<HTMLElement>("#info-engine-label")!.textContent = t(language, "infoEngine");
  document.querySelector<HTMLElement>("#info-platform-label")!.textContent = t(language, "infoPlatform");
  document.querySelector<HTMLElement>("#info-project-label")!.textContent = t(language, "infoProject");
  document.querySelector<HTMLElement>("#info-authors-label")!.textContent = t(language, "infoAuthors");
  document.querySelector<HTMLInputElement>("#root")!.placeholder = t(language, "chooseRoot");
  document.querySelector<HTMLElement>(".hero-title-en .hero-title-tail")!.textContent = t(language, "enTitleTail");
  document.querySelector<HTMLElement>(".hero-title-zh .hero-title-tail")!.textContent = t(language, "zhTitleTail");
  browseButton.textContent = t(language, "browse");
  deleteButton.textContent = t(language, "deleteSelected");
  updateModeHint();
  applyTheme((document.documentElement.dataset.theme as "light" | "dark") ?? "light");
  updateScanButton();
  setBadge(statusBadge.dataset.state ?? "idle");
  renderStatus();
  renderResults();
}

function setActivity(nextActivity: Activity) {
  activity = nextActivity;
  updateControls();
}

function updateControls() {
  const editingLocked = activity !== "idle";
  const canStop = activity === "scanning";
  scanButton.disabled = activity === "stopping" || activity === "deleting";
  deleteButton.disabled = editingLocked || selected.size === 0;
  rootInput.disabled = editingLocked;
  browseButton.disabled = editingLocked;
  modeSelect.disabled = editingLocked;
  if (canStop) {
    scanButton.disabled = false;
  }
}

function updateScanButton() {
  if (activity === "scanning") {
    scanButton.textContent = t(language, "stop");
  } else if (activity === "stopping") {
    scanButton.textContent = t(language, "stopping");
  } else if (hasCompletedScan) {
    scanButton.textContent = t(language, "rescan");
  } else {
    scanButton.textContent = t(language, "scan");
  }
}

function updateModeHint() {
  const hint = document.querySelector<HTMLElement>("#mode-hint");
  if (!hint) {
    return;
  }
  hint.textContent = t(language, keyForMode(modeSelect.value));
}

function isHighRiskRoot(root: string) {
  const normalized = root.trim().replaceAll("/", "\\");
  return /^[A-Za-z]:\\?$/.test(normalized) || /^\\\\[^\\]+\\[^\\]+\\?$/.test(normalized);
}

function formatDuration(durationMs: number) {
  return durationMs >= 1000 ? `${(durationMs / 1000).toFixed(2)} s` : `${durationMs} ms`;
}

function setStatusMessage(message: string, rawPayload?: unknown) {
  statusText.textContent = message;
  if (rawPayload !== undefined) {
    latestRawPayload = rawPayload;
  }
  statusRaw.textContent = JSON.stringify(latestRawPayload, null, 2);
}

function createStatusCard(label: string, value: string | number, subtle?: string) {
  const article = document.createElement("article");
  article.className = "status-card";
  article.innerHTML = `
    <span class="status-card-label">${label}</span>
    <strong class="status-card-value">${value}</strong>
    ${subtle ? `<span class="status-card-subtle">${subtle}</span>` : ""}
  `;
  return article;
}

function renderStatus() {
  statusCards.innerHTML = "";

  if (latestResult) {
    const { summary, deleteTargets } = latestResult;
    statusCards.append(
      createStatusCard(t(language, "cardsRoot"), summary.root),
      createStatusCard(t(language, "cardsMode"), summary.mode.toLowerCase()),
      createStatusCard(t(language, "cardsMatches"), summary.matchedCount),
      createStatusCard(t(language, "cardsDeleteTargets"), deleteTargets.length),
      createStatusCard(t(language, "cardsErrors"), summary.errorCount),
      createStatusCard(t(language, "cardsDuration"), formatDuration(summary.durationMs)),
    );
    return;
  }

  if (latestDelete) {
    statusCards.append(
      createStatusCard(t(language, "cardsDeleted"), latestDelete.summary.deletedCount),
      createStatusCard(t(language, "cardsFailedDeletes"), latestDelete.summary.failedCount),
      createStatusCard(t(language, "cardsDeleteTargets"), latestDelete.summary.requestedCount),
    );
    return;
  }

  if (shellStatus) {
    statusCards.append(
      createStatusCard(t(language, "cardsRoot"), shellStatus.currentDir),
      createStatusCard(t(language, "cardsMode"), shellStatus.mode),
      createStatusCard(t(language, "cardsDuration"), shellStatus.buildChannel),
    );
  }
}

function renderResults() {
  resultList.innerHTML = "";
  resultsSummary.textContent = latestResult
    ? `${latestResult.summary.matchedCount} · ${t(language, "cardsDeleteTargets")}: ${latestResult.deleteTargets.length} · ${t(language, "cardsErrors")}: ${latestResult.summary.errorCount}`
    : t(language, "noScanYet");

  if (!latestResult || latestResult.matches.length === 0) {
    const empty = document.createElement("li");
    empty.className = "empty-state";
    empty.textContent = t(language, "noScanYet");
    resultList.appendChild(empty);
    return;
  }

  const deleteTargets = new Set(latestResult.deleteTargets.map((item) => item.absolutePath));
  for (const item of latestResult.matches) {
    const deletable = deleteTargets.has(item.absolutePath);
    const li = document.createElement("li");
    li.className = `result-item ${deletable ? "result-item-delete-target" : ""}`;

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = selected.has(item.absolutePath);
    checkbox.disabled = !deletable || activity !== "idle";
    checkbox.addEventListener("change", () => {
      if (checkbox.checked) {
        selected.add(item.absolutePath);
      } else {
        selected.delete(item.absolutePath);
      }
      updateControls();
    });

    const meta = document.createElement("div");
    meta.className = "result-meta";

    const path = document.createElement("div");
    path.className = "result-path";
    path.textContent = item.relativePath || item.absolutePath;

    const subtle = document.createElement("div");
    subtle.className = "result-subtle";
    subtle.textContent = `${item.sizeBytes} ${t(language, "bytes")} · ${deletable ? t(language, "deleteTarget") : t(language, "scanOnlyMatch")}`;

    meta.append(path, subtle);
    li.append(checkbox, meta);
    resultList.appendChild(li);
  }
}

function populateInfoDrawer(status: ShellStatus) {
  document.querySelector<HTMLElement>("#info-build-value")!.textContent = status.buildChannel;
  document.querySelector<HTMLElement>("#info-version-value")!.textContent = status.version;
  document.querySelector<HTMLElement>("#info-engine-value")!.textContent = status.engine;
  document.querySelector<HTMLElement>("#info-platform-value")!.textContent = `${status.os} / ${status.arch}`;
  document.querySelector<HTMLElement>("#info-project-value")!.textContent = status.projectUrl || t(language, "infoPendingProject");

  const authors = document.querySelector<HTMLUListElement>("#info-authors-value")!;
  authors.innerHTML = "";
  for (const author of status.authors) {
    const li = document.createElement("li");
    li.textContent = author;
    authors.appendChild(li);
  }
}

async function runScan() {
  const root = rootInput.value.trim();
  if (!root) {
    setBadge("needs-root");
    setStatusMessage(t(language, "chooseRoot"), { reason: "missing-root" });
    return;
  }

  setActivity("scanning");
  updateScanButton();
  setBadge("scanning");
  selected = new Set();
  latestDelete = null;
  setStatusMessage(t(language, "scanningRoot", { root }), { activity: "scan", root, mode: modeSelect.value });

  try {
    latestResult = await invoke<ScanResult>("scan", {
      request: {
        root,
        mode: modeSelect.value,
      },
    });

    hasCompletedScan = true;
    setActivity("idle");
    updateScanButton();
    setBadge(latestResult.summary.errorCount > 0 ? "partial" : "ready");
    setStatusMessage(
      `${latestResult.summary.matchedCount} ${t(language, "matches").toLowerCase()} · ${latestResult.deleteTargets.length} ${t(language, "cardsDeleteTargets").toLowerCase()} · ${formatDuration(latestResult.summary.durationMs)}`,
      latestResult,
    );
    renderStatus();
    renderResults();
  } catch (error) {
    setActivity("idle");
    updateScanButton();
    if (String(error).includes("scan-cancelled")) {
      latestResult = null;
      selected = new Set();
      hasCompletedScan = false;
      setBadge("idle");
      setStatusMessage(t(language, "scanCancelled"), { cancelled: true, root, mode: modeSelect.value });
      renderStatus();
      renderResults();
      return;
    }

    setBadge("failed");
    setStatusMessage(String(error), { error: String(error) });
  }
}

async function requestScan(userInitiated: boolean) {
  const root = rootInput.value.trim();
  if (!root) {
    setBadge("needs-root");
    setStatusMessage(t(language, "chooseRoot"));
    return;
  }

  if (isHighRiskRoot(root)) {
    if (!userInitiated) {
      setBadge("guarded");
      setStatusMessage(t(language, "guardedAuto", { root }), { guarded: true, root });
      return;
    }

    const confirmed = window.confirm(t(language, "guardedConfirm", { root }));
    if (!confirmed) {
      setBadge("guarded");
      setStatusMessage(t(language, "guardedCancel", { root }), { guarded: true, root });
      return;
    }
  }

  await runScan();
}

async function stopScan() {
  setActivity("stopping");
  updateScanButton();
  setBadge("stopping");
  setStatusMessage(t(language, "stoppingScan"), { cancelling: true });
  await invoke<boolean>("cancel_scan");
}

scanButton.addEventListener("click", () => {
  if (activity === "scanning") {
    void stopScan();
    return;
  }
  if (activity === "idle") {
    void requestScan(true);
  }
});

deleteButton.addEventListener("click", async () => {
  if (!latestResult || selected.size === 0) {
    return;
  }

  const matches = latestResult.deleteTargets.filter((item) => selected.has(item.absolutePath));
  setActivity("deleting");
  updateScanButton();
  setBadge("deleting");
  setStatusMessage(t(language, "deletingSelected", { count: matches.length }), { deleting: matches });

  try {
    const result = await invoke<DeleteResult>("delete", { matches });
    latestDelete = result;
    setStatusMessage(
      `${result.summary.deletedCount} ${t(language, "cardsDeleted").toLowerCase()} · ${result.summary.failedCount} ${t(language, "cardsFailedDeletes").toLowerCase()}`,
      result,
    );
    setActivity("idle");
    await requestScan(true);
    renderStatus();
  } catch (error) {
    setActivity("idle");
    setBadge("failed");
    setStatusMessage(String(error), { error: String(error) });
  }
});

modeSelect.addEventListener("change", () => {
  updateModeHint();
});

browseButton.addEventListener("click", async () => {
  const picked = await invoke<string | null>("pick_root_dir");
  if (picked) {
    rootInput.value = picked;
  }
});

themeToggle.addEventListener("click", () => {
  applyTheme(document.documentElement.dataset.theme === "dark" ? "light" : "dark");
});

langToggle.addEventListener("click", () => {
  setLanguage(language === "en" ? "zh" : "en");
});

infoToggle.addEventListener("click", () => {
  infoDrawer.classList.toggle("hidden");
  document.querySelector<HTMLElement>("#info-toggle-label")!.textContent = infoDrawer.classList.contains("hidden") ? t(language, "infoToggle") : t(language, "infoHide");
});

document.querySelector<HTMLButtonElement>("#minimize-btn")!.addEventListener("click", () => {
  void appWindow.minimize();
});

document.querySelector<HTMLButtonElement>("#maximize-btn")!.addEventListener("click", async () => {
  if (await appWindow.isMaximized()) {
    await appWindow.unmaximize();
  } else {
    await appWindow.maximize();
  }
});

document.querySelector<HTMLButtonElement>("#close-btn")!.addEventListener("click", () => {
  void appWindow.close();
});

initializeTheme();
setLanguage(language);
renderStatus();
renderResults();

invoke<ShellStatus>("shell_status")
  .then((result) => {
    shellStatus = result;
    rootInput.value = result.currentDir;
    latestRawPayload = result;
    populateInfoDrawer(result);
    setBadge("ready");
    setStatusMessage(t(language, "statusShell"), result);
    renderStatus();
    void requestScan(false);
  })
  .catch((error) => {
    setBadge("failed");
    setStatusMessage(String(error), { error: String(error) });
  });

rawDetails.open = false;
