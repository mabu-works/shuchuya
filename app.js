"use strict";

/* ============================================================
   しゅうちゅうや ― ポモドーロ式 集中タイマー
   集中と休憩を自動で切り替え、実績を localStorage に記録します。
   ============================================================ */

const SETTINGS_KEY = "shuchuya-settings-v1";
const STATS_KEY = "shuchuya-stats-v1";
const DEFAULT_TITLE = document.title;
const RING_C = 2 * Math.PI * 88; // SVG 円の円周（r=88）

/* ---------- 設定 ---------- */

function clamp(n, min, max, fallback) {
  n = Number(n);
  if (!Number.isFinite(n)) return fallback;
  return Math.min(max, Math.max(min, Math.round(n)));
}

function loadSettings() {
  try {
    const parsed = JSON.parse(localStorage.getItem(SETTINGS_KEY) || "{}");
    return {
      work: clamp(parsed.work, 5, 90, 25),
      break: clamp(parsed.break, 1, 30, 5),
    };
  } catch (e) {
    return { work: 25, break: 5 };
  }
}

const settings = loadSettings();

function saveSettings() {
  try {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
  } catch (e) {}
}

/* ---------- 統計 ---------- */

function dateKey(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return y + "-" + m + "-" + day;
}

function loadStats() {
  try {
    return JSON.parse(localStorage.getItem(STATS_KEY) || "{}");
  } catch (e) {
    return {};
  }
}

const stats = loadStats();

function saveStats() {
  try {
    localStorage.setItem(STATS_KEY, JSON.stringify(stats));
  } catch (e) {}
}

function recordWorkSession() {
  const key = dateKey(new Date());
  if (!stats[key]) stats[key] = { sessions: 0, minutes: 0 };
  stats[key].sessions += 1;
  stats[key].minutes += settings.work;
  saveStats();
  renderStats();
}

/* ---------- 通知音（Web Audio） ---------- */

let audioCtx = null;
function ensureAudio() {
  if (!audioCtx) {
    const AC = window.AudioContext || window.webkitAudioContext;
    if (AC) audioCtx = new AC();
  }
  if (audioCtx && audioCtx.state === "suspended") audioCtx.resume();
}

function playChime(kind) {
  ensureAudio();
  if (!audioCtx) return;
  const notes = kind === "work" ? [880, 1174.66] : [1174.66, 880];
  const t0 = audioCtx.currentTime + 0.02;
  notes.forEach((freq, i) => {
    const osc = audioCtx.createOscillator();
    osc.type = "sine";
    osc.frequency.value = freq;
    const g = audioCtx.createGain();
    const start = t0 + i * 0.22;
    g.gain.setValueAtTime(0, start);
    g.gain.linearRampToValueAtTime(0.22, start + 0.02);
    g.gain.exponentialRampToValueAtTime(0.001, start + 0.35);
    osc.connect(g).connect(audioCtx.destination);
    osc.start(start);
    osc.stop(start + 0.4);
  });
}

/* ---------- タイマー本体 ---------- */

let phase = "work"; // "work" | "break"
let totalSec = settings.work * 60;
let remainSec = totalSec;
let running = false;
let endAt = null;
let tickId = null;

function phaseDuration(p) {
  return (p === "work" ? settings.work : settings.break) * 60;
}

function fmt(sec) {
  const m = Math.floor(sec / 60);
  const s = sec % 60;
  return String(m).padStart(2, "0") + ":" + String(s).padStart(2, "0");
}

function startTimer() {
  ensureAudio();
  running = true;
  endAt = Date.now() + remainSec * 1000;
  if (tickId) clearInterval(tickId);
  tickId = setInterval(tick, 200);
  updateUI();
}

function pauseTimer() {
  running = false;
  if (endAt) remainSec = Math.max(0, Math.round((endAt - Date.now()) / 1000));
  endAt = null;
  if (tickId) {
    clearInterval(tickId);
    tickId = null;
  }
  updateUI();
}

function resetTimer() {
  running = false;
  endAt = null;
  if (tickId) {
    clearInterval(tickId);
    tickId = null;
  }
  phase = "work";
  totalSec = phaseDuration(phase);
  remainSec = totalSec;
  document.title = DEFAULT_TITLE;
  updateUI();
}

function tick() {
  if (!endAt) return;
  remainSec = Math.max(0, Math.round((endAt - Date.now()) / 1000));
  if (remainSec <= 0) {
    onPhaseEnd();
    return;
  }
  updateUI();
}

function onPhaseEnd() {
  if (phase === "work") {
    recordWorkSession();
    playChime("work");
    phase = "break";
  } else {
    playChime("break");
    phase = "work";
  }
  totalSec = phaseDuration(phase);
  remainSec = totalSec;
  endAt = Date.now() + remainSec * 1000; // 自動で次のフェーズを開始
  updateUI();
}

/* ---------- UI ---------- */

const phaseLabel = document.getElementById("phaseLabel");
const timeText = document.getElementById("timeText");
const stateText = document.getElementById("stateText");
const ringFg = document.getElementById("ringFg");
const startPauseBtn = document.getElementById("startPauseBtn");
const resetBtn = document.getElementById("resetBtn");
const workInput = document.getElementById("workInput");
const breakInput = document.getElementById("breakInput");
const todaySessions = document.getElementById("todaySessions");
const todayMinutes = document.getElementById("todayMinutes");
const weekChart = document.getElementById("weekChart");

ringFg.style.strokeDasharray = String(RING_C);
ringFg.style.strokeDashoffset = "0";

function updateUI() {
  const isBreak = phase === "break";
  phaseLabel.textContent = isBreak ? "休憩" : "集中";
  phaseLabel.classList.toggle("break", isBreak);
  ringFg.classList.toggle("break", isBreak);
  timeText.textContent = fmt(remainSec);

  const fraction = totalSec > 0 ? remainSec / totalSec : 0;
  ringFg.style.strokeDashoffset = String(RING_C * (1 - fraction));

  stateText.textContent = running
    ? "実行中"
    : remainSec < totalSec
      ? "一時停止中"
      : "待機中";
  startPauseBtn.textContent = running
    ? "⏸ 一時停止"
    : remainSec < totalSec
      ? "▶ 再開"
      : "▶ 開始";

  // タブタイトルに残り時間を表示（待機中は既定タイトル）
  if (!running && remainSec === totalSec && phase === "work") {
    document.title = DEFAULT_TITLE;
  } else {
    document.title =
      fmt(remainSec) + " " + (isBreak ? "休憩" : "集中") + "｜しゅうちゅうや";
  }
}

startPauseBtn.addEventListener("click", () => {
  if (running) pauseTimer();
  else startTimer();
});

resetBtn.addEventListener("click", resetTimer);

/* ---------- 設定 UI ---------- */

function applySettingsFromInputs() {
  settings.work = clamp(workInput.value, 5, 90, 25);
  settings.break = clamp(breakInput.value, 1, 30, 5);
  workInput.value = String(settings.work);
  breakInput.value = String(settings.break);
  saveSettings();
  if (!running) {
    totalSec = phaseDuration(phase);
    remainSec = totalSec;
    endAt = null;
  }
  updateUI();
}

workInput.addEventListener("change", applySettingsFromInputs);
breakInput.addEventListener("change", applySettingsFromInputs);

/* ---------- 統計表示 ---------- */

const WEEKDAYS = ["日", "月", "火", "水", "木", "金", "土"];

function renderStats() {
  const today = stats[dateKey(new Date())] || { sessions: 0, minutes: 0 };
  todaySessions.textContent = String(today.sessions);
  todayMinutes.textContent = today.minutes + "分";

  // 直近7日（今日を含む）
  const days = [];
  for (let i = 6; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    const entry = stats[dateKey(d)] || { sessions: 0, minutes: 0 };
    days.push({ date: d, minutes: entry.minutes, isToday: i === 0 });
  }
  const max = Math.max(1, ...days.map((d) => d.minutes));

  weekChart.innerHTML = "";
  for (const day of days) {
    const col = document.createElement("div");
    col.className = "bar-col";

    const value = document.createElement("span");
    value.className = "bar-value";
    value.textContent = day.minutes > 0 ? day.minutes + "分" : "";

    const bar = document.createElement("div");
    bar.className = "bar" + (day.isToday ? " today" : "");
    bar.style.height = Math.round((day.minutes / max) * 100) + "%";

    const label = document.createElement("span");
    label.className = "bar-label";
    label.textContent =
      day.date.getMonth() +
      1 +
      "/" +
      day.date.getDate() +
      "(" +
      WEEKDAYS[day.date.getDay()] +
      ")";

    col.append(value, bar, label);
    weekChart.appendChild(col);
  }
}

/* ---------- 初期化 ---------- */

workInput.value = String(settings.work);
breakInput.value = String(settings.break);
updateUI();
renderStats();

/* ---------- テーマ切り替え ---------- */

(function initTheme() {
  var root = document.documentElement;
  if (!root || !root.getAttribute || !root.setAttribute) return;
  function apply(theme) {
    root.setAttribute("data-theme", theme);
    var btn = document.getElementById("themeToggle");
    if (btn) btn.textContent = theme === "light" ? "☀️" : "🌙";
  }
  var current = root.getAttribute("data-theme");
  if (current !== "light" && current !== "dark") {
    try { current = localStorage.getItem("shuchuya-theme"); } catch (e) { current = null; }
  }
  apply(current === "light" ? "light" : "dark");
  var btn = document.getElementById("themeToggle");
  if (btn && btn.addEventListener) {
    btn.addEventListener("click", function () {
      var next = root.getAttribute("data-theme") === "light" ? "dark" : "light";
      apply(next);
      try { localStorage.setItem("shuchuya-theme", next); } catch (e) { /* 保存できなくても切り替え自体は有効 */ }
    });
  }
})();
