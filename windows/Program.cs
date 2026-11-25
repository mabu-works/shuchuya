using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows.Forms;

namespace Shuchuya
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    // ==========================================
// 💾 保存データ（Web版の localStorage の代わりに JSON ファイル）
// ==========================================
    internal sealed class DayStat
    {
        public int Sessions { get; set; }
        public int Minutes { get; set; }
    }

    internal sealed class AppData
    {
        public int Work { get; set; } = 25;
        public int Break { get; set; } = 5;
        public Dictionary<string, DayStat> Stats { get; set; } = new Dictionary<string, DayStat>();
    }

    // ==========================================
// ⭕ 進捗リング（Web版の SVG リングを GDI+ で描画）
// ==========================================
    internal sealed class RingPanel : Panel
    {
        public string PhaseText = "集中";
        public string TimeText = "25:00";
        public string StateText = "待機中";
        public double Fraction = 1.0;
        public bool IsBreak = false;

        private static readonly Color Track = Color.FromArgb(38, 54, 80);      // --track #263650
        private static readonly Color Accent = Color.FromArgb(106, 141, 255);  // 集中
        private static readonly Color BreakColor = Color.FromArgb(45, 212, 191); // 休憩
        private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
        private static readonly Color Muted = Color.FromArgb(157, 177, 199);

        public RingPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 24;
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;
            var rect = new Rectangle(x, y, size, size);

            using (var pen = new Pen(Track, 12))
                g.DrawEllipse(pen, rect);
            float sweep = (float)(360.0 * Math.Max(0.0, Math.Min(1.0, Fraction)));
            if (sweep > 0.01f)
            {
                using (var pen = new Pen(IsBreak ? BreakColor : Accent, 12))
                    g.DrawArc(pen, rect, -90f, sweep);
            }

            using (var f = new Font("Yu Gothic UI", 13F, FontStyle.Bold))
            using (var b = new SolidBrush(IsBreak ? BreakColor : Accent))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(PhaseText, f, b, new RectangleF(0, Height / 2 - 58, Width, 26), sf);
            }
            using (var f = new Font("Yu Gothic UI", 34F, FontStyle.Bold))
            using (var b = new SolidBrush(TextColor))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(TimeText, f, b, new RectangleF(0, Height / 2 - 34, Width, 56), sf);
            }
            using (var f = new Font("Yu Gothic UI", 10F))
            using (var b = new SolidBrush(Muted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(StateText, f, b, new RectangleF(0, Height / 2 + 28, Width, 24), sf);
            }
        }
    }

    // ==========================================
// 📊 直近7日の棒グラフ
// ==========================================
    internal sealed class WeekChartPanel : Panel
    {
        public List<(string Label, int Minutes, bool IsToday)> Days = new List<(string, int, bool)>();

        private static readonly Color BarColor = Color.FromArgb(56, 76, 120);
        private static readonly Color TodayColor = Color.FromArgb(106, 141, 255);
        private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
        private static readonly Color Muted = Color.FromArgb(157, 177, 199);

        public WeekChartPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Days.Count == 0) return;
            var g = e.Graphics;
            int max = 1;
            foreach (var d in Days) max = Math.Max(max, d.Minutes);

            float colW = (float)Width / Days.Count;
            const int barAreaTop = 22;
            const int barAreaBottom = 28;
            int barAreaH = Height - barAreaTop - barAreaBottom;

            using (var valueFont = new Font("Yu Gothic UI", 8.5F))
            using (var labelFont = new Font("Yu Gothic UI", 8.5F))
            {
                for (int i = 0; i < Days.Count; i++)
                {
                    var day = Days[i];
                    float cx = i * colW;
                    float bw = Math.Min(44f, colW - 18f);
                    float bx = cx + (colW - bw) / 2;
                    int bh = (int)Math.Round((double)day.Minutes / max * barAreaH);
                    float by = barAreaTop + (barAreaH - bh);

                    using (var b = new SolidBrush(day.IsToday ? TodayColor : BarColor))
                        if (bh > 0) g.FillRectangle(b, bx, by, bw, bh);

                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    if (day.Minutes > 0)
                    {
                        using (var b = new SolidBrush(TextColor))
                            g.DrawString(day.Minutes + "分", valueFont, b, new RectangleF(cx, 2, colW, 18), sf);
                    }
                    using (var b = new SolidBrush(Muted))
                        g.DrawString(day.Label, labelFont, b, new RectangleF(cx, Height - barAreaBottom + 6, colW, 18), sf);
                }
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(10, 15, 28);
        private static readonly Color Surface = Color.FromArgb(17, 26, 44);
        private static readonly Color Surface2 = Color.FromArgb(24, 38, 66);
        private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
        private static readonly Color Muted = Color.FromArgb(157, 177, 199);
        private static readonly Color Accent = Color.FromArgb(106, 141, 255);

        private const string DefaultTitle = "しゅうちゅうや for Windows｜集中タイマー";
        private static readonly string[] Weekdays = { "日", "月", "火", "水", "木", "金", "土" };

        private readonly RingPanel ring;
        private readonly Button startPauseBtn;
        private readonly NumericUpDown workInput;
        private readonly NumericUpDown breakInput;
        private readonly Label todaySessionsLabel;
        private readonly Label todayMinutesLabel;
        private readonly WeekChartPanel weekChart;
        private readonly Timer timer;

        private AppData data;
        private string phase = "work"; // "work" | "break"
        private int totalSec;
        private int remainSec;
        private bool running = false;
        private DateTime? endAt = null;

        public MainForm()
        {
            data = LoadData();
            totalSec = PhaseDuration(phase);
            remainSec = totalSec;

            Text = DefaultTitle;
            ClientSize = new Size(480, 730);
            MinimumSize = new Size(496, 769);
            BackColor = Bg;
            ForeColor = TextColor;
            Font = new Font("Yu Gothic UI", 10F);

            var title = new Label
            {
                Text = "しゅうちゅうや｜集中タイマー",
                Font = new Font("Yu Gothic UI", 15F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, 12),
                AutoSize = true
            };
            Controls.Add(title);

            ring = new RingPanel
            {
                Location = new Point(120, 52),
                Size = new Size(240, 240),
                BackColor = Bg
            };
            Controls.Add(ring);

            startPauseBtn = new Button
            {
                Text = "▶ 開始",
                Font = new Font("Yu Gothic UI", 11F, FontStyle.Bold),
                BackColor = Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(90, 306),
                Size = new Size(150, 40)
            };
            startPauseBtn.FlatAppearance.BorderSize = 0;
            startPauseBtn.Click += (s, e) => { if (running) PauseTimer(); else StartTimer(); };
            Controls.Add(startPauseBtn);

            var resetBtn = new Button
            {
                Text = "リセット",
                BackColor = Surface2,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(252, 306),
                Size = new Size(130, 40)
            };
            resetBtn.Click += (s, e) => ResetTimer();
            Controls.Add(resetBtn);

            Controls.Add(MakeLabel("設定", 20, 366));
            Controls.Add(MakeLabel("集中（分）", 20, 394));
            workInput = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 90,
                Value = data.Work,
                Location = new Point(110, 390),
                Width = 80,
                BackColor = Surface2,
                ForeColor = TextColor
            };
            workInput.ValueChanged += (s, e) => ApplySettings();
            Controls.Add(workInput);

            Controls.Add(MakeLabel("休憩（分）", 230, 394));
            breakInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 30,
                Value = data.Break,
                Location = new Point(320, 390),
                Width = 80,
                BackColor = Surface2,
                ForeColor = TextColor
            };
            breakInput.ValueChanged += (s, e) => ApplySettings();
            Controls.Add(breakInput);

            Controls.Add(MakeLabel("今日の実績", 20, 440));
            todaySessionsLabel = new Label
            {
                ForeColor = TextColor,
                Location = new Point(20, 466),
                AutoSize = true
            };
            Controls.Add(todaySessionsLabel);
            todayMinutesLabel = new Label
            {
                ForeColor = TextColor,
                Location = new Point(230, 466),
                AutoSize = true
            };
            Controls.Add(todayMinutesLabel);

            Controls.Add(MakeLabel("直近7日", 20, 500));
            weekChart = new WeekChartPanel
            {
                Location = new Point(20, 526),
                Size = new Size(440, 150),
                BackColor = Surface
            };
            Controls.Add(weekChart);

            timer = new Timer { Interval = 200 };
            timer.Tick += (s, e) => Tick();

            UpdateUI();
            RenderStats();
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, ForeColor = Muted, Location = new Point(x, y), AutoSize = true };
        }

        // ==========================================
// ⏱️ タイマー本体（Web版と同じ endAt 方式）
// ==========================================
        private int PhaseDuration(string p) => (p == "work" ? data.Work : data.Break) * 60;

        private static string Fmt(int sec)
        {
            return (sec / 60).ToString("D2") + ":" + (sec % 60).ToString("D2");
        }

        private static int RoundSeconds(double ms)
        {
            return (int)Math.Floor(ms / 1000.0 + 0.5);
        }

        private void StartTimer()
        {
            running = true;
            endAt = DateTime.Now.AddSeconds(remainSec);
            timer.Start();
            UpdateUI();
        }

        private void PauseTimer()
        {
            running = false;
            if (endAt.HasValue) remainSec = Math.Max(0, RoundSeconds((endAt.Value - DateTime.Now).TotalMilliseconds));
            endAt = null;
            timer.Stop();
            UpdateUI();
        }

        private void ResetTimer()
        {
            running = false;
            endAt = null;
            timer.Stop();
            phase = "work";
            totalSec = PhaseDuration(phase);
            remainSec = totalSec;
            Text = DefaultTitle;
            UpdateUI();
        }

        private void Tick()
        {
            if (!endAt.HasValue) return;
            remainSec = Math.Max(0, RoundSeconds((endAt.Value - DateTime.Now).TotalMilliseconds));
            if (remainSec <= 0)
            {
                OnPhaseEnd();
                return;
            }
            UpdateUI();
        }

        private void OnPhaseEnd()
        {
            if (phase == "work")
            {
                RecordWorkSession();
                SystemSounds.Exclamation.Play();
                phase = "break";
            }
            else
            {
                SystemSounds.Asterisk.Play();
                phase = "work";
            }
            totalSec = PhaseDuration(phase);
            remainSec = totalSec;
            endAt = DateTime.Now.AddSeconds(remainSec); // 自動で次のフェーズを開始
            UpdateUI();
        }

        private void ApplySettings()
        {
            data.Work = (int)workInput.Value;
            data.Break = (int)breakInput.Value;
            SaveData();
            if (!running)
            {
                totalSec = PhaseDuration(phase);
                remainSec = totalSec;
                endAt = null;
            }
            UpdateUI();
        }

        // ==========================================
// 🖥️ UI 更新
// ==========================================
        private void UpdateUI()
        {
            bool isBreak = phase == "break";
            ring.PhaseText = isBreak ? "休憩" : "集中";
            ring.IsBreak = isBreak;
            ring.TimeText = Fmt(remainSec);
            ring.Fraction = totalSec > 0 ? (double)remainSec / totalSec : 0;
            ring.StateText = running ? "実行中" : (remainSec < totalSec ? "一時停止中" : "待機中");
            ring.Invalidate();

            startPauseBtn.Text = running ? "⏸ 一時停止" : (remainSec < totalSec ? "▶ 再開" : "▶ 開始");

            // 🕐 タイトルバーに残り時間を表示（待機中は既定タイトル）
            if (!running && remainSec == totalSec && phase == "work")
            {
                Text = DefaultTitle;
            }
            else
            {
                Text = Fmt(remainSec) + " " + (isBreak ? "休憩" : "集中") + "｜しゅうちゅうや";
            }
        }

        // ==========================================
// 📊 統計
// ==========================================
        private static string DateKey(DateTime d) => d.ToString("yyyy-MM-dd");

        private void RecordWorkSession()
        {
            string key = DateKey(DateTime.Now);
            if (!data.Stats.TryGetValue(key, out var entry))
            {
                entry = new DayStat();
                data.Stats[key] = entry;
            }
            entry.Sessions += 1;
            entry.Minutes += data.Work;
            SaveData();
            RenderStats();
        }

        private void RenderStats()
        {
            var today = data.Stats.TryGetValue(DateKey(DateTime.Now), out var t) ? t : new DayStat();
            todaySessionsLabel.Text = "セッション: " + today.Sessions + " 回";
            todayMinutesLabel.Text = "集中時間: " + today.Minutes + "分";

            var days = new List<(string, int, bool)>();
            for (int i = 6; i >= 0; i--)
            {
                var d = DateTime.Now.Date.AddDays(-i);
                var entry = data.Stats.TryGetValue(DateKey(d), out var e) ? e : new DayStat();
                string label = $"{d.Month}/{d.Day}({Weekdays[(int)d.DayOfWeek]})";
                days.Add((label, entry.Minutes, i == 0));
            }
            weekChart.Days = days;
            weekChart.Invalidate();
        }

        // ==========================================
// 💾 保存・読み込み
// ==========================================
        private static string DataPath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shuchuya");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "data.json");
        }

        private AppData LoadData()
        {
            try
            {
                string path = DataPath();
                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<AppData>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        loaded.Work = Math.Min(90, Math.Max(5, loaded.Work));
                        loaded.Break = Math.Min(30, Math.Max(1, loaded.Break));
                        loaded.Stats ??= new Dictionary<string, DayStat>();
                        return loaded;
                    }
                }
            }
            catch (Exception)
            {
                // 📥 読み込めない場合は既定値で始める（Web版と同じ扱い）
            }
            return new AppData();
        }

        private void SaveData()
        {
            try
            {
                File.WriteAllText(DataPath(), JsonSerializer.Serialize(data));
            }
            catch (Exception)
            {
                // 💾 保存できない環境では無視
            }
        }
    }
}
