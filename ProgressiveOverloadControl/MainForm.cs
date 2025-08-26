using ProgressiveOverloadControl.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ProgressiveOverloadControl
{
    public record SetEntry(DateTime Date, string Exercise, int SetNo, int Reps, double WeightKg, int? RIR = null)
    {
        public double Volume => Reps * WeightKg;
        public double Est1Rm => Reps <= 1 ? WeightKg : WeightKg * (1.0 + Reps / 30.0); // Epley
        public int IsoYear => ISOWeek.GetYear(Date);
        public int IsoWeek => ISOWeek.GetWeekOfYear(Date);
    }

    public record WeeklyExerciseStats(string Exercise, int IsoYear, int IsoWeek,
        int TotalSets, int TotalReps, double TotalVolume, double TopSet1Rm, double? AvgRIR);

    public partial class MainForm : Form
    {
        private GymContext? _db;
        private readonly BindingList<SetEntry> _log = new();
        private readonly BindingList<WeeklyExerciseStats> _weekly = new();

        private static SetEntry ToSetEntry(SetLogEntity e) =>
    new(e.Date, e.Exercise, e.SetNo, e.Reps, e.WeightKg, e.RIR);

        private static SetLogEntity ToEntity(SetEntry s) => new()
        {
            Date = s.Date,
            Exercise = s.Exercise,
            SetNo = s.SetNo,
            Reps = s.Reps,
            WeightKg = s.WeightKg,
            RIR = s.RIR
        };

        // 🔸 Kapsamlı egzersiz kataloğu
        private static readonly string[] ExerciseCatalog = new[]
        {
            // Alt vücut
            "Back Squat","Front Squat","Goblet Squat","Hack Squat","Leg Press",
            "Deadlift","Romanian Deadlift","Sumo Deadlift","Bulgarian Split Squat",
            "Walking Lunge","Reverse Lunge","Hip Thrust","Glute Bridge",
            "Leg Extension","Leg Curl","Seated Leg Curl","Calf Raise","Standing Calf Raise",

            // Üst vücut itiş
            "Bench Press","Incline Bench Press","Dumbbell Bench Press","Incline DB Press",
            "Overhead Press","Seated DB Shoulder Press","Dips","Push-up","Cable Fly","Incline Cable Fly",

            // Üst vücut çekiş
            "Barbell Row","Pendlay Row","Seated Cable Row","T-Bar Row",
            "Lat Pulldown","Pull-up","Chin-up","Face Pull","Straight-Arm Pulldown",

            // Omuz izolasyon
            "Lateral Raise","Rear Delt Fly","Front Raise",

            // Kol
            "Barbell Curl","Dumbbell Curl","Hammer Curl","Incline DB Curl",
            "Triceps Pushdown","Skullcrusher","Overhead Triceps Extension",

            // Core
            "Crunch","Hanging Leg Raise","Cable Crunch","Plank"
        };

        public MainForm()
        {
            InitializeComponent();

            // Log/weekly datasource
            gridLog.DataSource = _log;
            gridWeekly.DataSource = _weekly;

            // 🔸 Egzersiz giriş combobox’ını katalogla doldur
            cmbExInput.Items.AddRange(ExerciseCatalog.OrderBy(x=>x).ToArray());
            cmbExInput.SelectedIndex = cmbExInput.Items.Count > 0 ? 0 : -1;

            // events
            btnAdd.Click += (_, __) => AddEntry();
            btnDelete.Click += (_, __) => DeleteSelected();
            btnRecalc.Click += (_, __) => Recalculate();
            cmbExerciseForChart.SelectedIndexChanged += (_, __) => DrawChart();
            cmbMetric.SelectedIndexChanged += (_, __) => DrawChart();

            SeedProfiles();

            // >>> DB init & load
            _db = new GymContext();

            LoadFromDb();   // DB -> _log
            Recalculate();

            // Form kapanınca context'i kapat
            this.FormClosing += (_, __) => _db?.Dispose();
        }
        private void LoadFromDb()
        {
            _log.Clear();
            // Tarihe göre getir
            var list = _db!.SetLogs
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Exercise)
                .ThenBy(x => x.SetNo)
                .ToList();

            foreach (var e in list)
                _log.Add(ToSetEntry(e));
        }
        private readonly Dictionary<string, ExerciseProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

        private void AddEntry()
        {
            try
            {
                // Girdiler
                if (!int.TryParse(txtSet.Text, out int setVal) || setVal <= 0) { MessageBox.Show("Set sayısı/geçerli set değeri girin."); return; }
                if (!int.TryParse(txtRep.Text, out int reps) || reps <= 0) { MessageBox.Show("Tekrar geçersiz."); return; }

                // Virgül/nokta farklarını tolere etmek istersen:
                if (!(double.TryParse(txtKg.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double kg) ||
                      double.TryParse(txtKg.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out kg)) || kg <= 0)
                {
                    MessageBox.Show("Kg geçersiz."); return;
                }

                int? rir = null;
                if (!string.IsNullOrWhiteSpace(txtRir.Text))
                {
                    if (!int.TryParse(txtRir.Text, out var rirVal) || rirVal < 0 || rirVal > 6) { MessageBox.Show("RIR 0–6 arası olmalı."); return; }
                    rir = rirVal;
                }

                var exName = (cmbExInput.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(exName))
                {
                    MessageBox.Show("Egzersiz seç veya yaz."); return;
                }
                if (!_profiles.ContainsKey(exName))
                    _profiles[exName] = GuessProfileFor(exName);

                // Kataloga yoksa ekle
                if (!cmbExInput.Items.Cast<string>().Any(i => i.Equals(exName, StringComparison.OrdinalIgnoreCase)))
                    cmbExInput.Items.Add(exName);

                var date = dtpDate.Value.Date;

                // Çoklu set senaryosu mu?
                bool multi = chkSameAllSets.Checked;
                int count = multi ? setVal : 1;

                // O gün/o egzersiz için mevcut en büyük SetNo'yu bul → devam numaralandır
                int existingMaxSetNo = _log
                    .Where(x => x.Date == date && x.Exercise.Equals(exName, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.SetNo)
                    .DefaultIfEmpty(0)
                    .Max();

                // DB toplu ekleme için buffer
                var toAdd = new List<SetEntry>(capacity: count);

                for (int i = 0; i < count; i++)
                {
                    int setNo = multi ? (existingMaxSetNo + 1 + i) : setVal; // multi: 1..N devam; tek: girilen setNo
                    var entry = new SetEntry(date, exName, setNo, reps, kg, rir);
                    toAdd.Add(entry);
                }

                // DB'ye yaz (varsa)
                if (_db != null)
                {
                    foreach (var e in toAdd)
                        _db.SetLogs.Add(ToEntity(e));
                    _db.SaveChanges();
                }

                // UI listesine ekle
                foreach (var e in toAdd)
                    _log.Add(e);

                Recalculate();

                // giriş temizliği
                if (multi) txtSet.Clear();
                txtRep.Clear(); txtKg.Clear(); txtRir.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }


        private void DeleteSelected()
        {
            if (gridLog.SelectedRows.Count == 0) return;
            var toRemove = gridLog.SelectedRows.Cast<DataGridViewRow>()
                           .Select(r => r.DataBoundItem as SetEntry).Where(x => x != null).ToList();
            if (toRemove.Count == 0) return;
            foreach (var s in toRemove)
            {
                var e = _db!.SetLogs.FirstOrDefault(x =>
                    x.Date == s!.Date &&
                    x.Exercise == s.Exercise &&
                    x.SetNo == s.SetNo &&
                    x.Reps == s.Reps &&
                    Math.Abs(x.WeightKg - s.WeightKg) < 0.0001 &&
                    x.RIR == s.RIR
                );

                if (e != null)
                    _db.SetLogs.Remove(e);
            }
            _db!.SaveChanges();

            // UI'dan çıkar
            foreach (var s in toRemove)
                _log.Remove(s!);

            Recalculate();
        }

        private void Recalculate()
        {
            _weekly.Clear();

            var grouped = _log.GroupBy(s => (s.Exercise, s.IsoYear, s.IsoWeek))
                              .OrderBy(g => g.Key.IsoYear).ThenBy(g => g.Key.IsoWeek).ThenBy(g => g.Key.Exercise);

            foreach (var g in grouped)
            {
                int totalSets = g.Count();
                int totalReps = g.Sum(x => x.Reps);
                double totalVol = Math.Round(g.Sum(x => x.Volume), 1);
                double top1Rm = Math.Round(g.Max(x => x.Est1Rm), 1);
                double? avgRir = g.Any(x => x.RIR.HasValue)
     ? Math.Round(g.Where(x => x.RIR.HasValue).Average(x => x.RIR!.Value), 1)
     : (double?)null;

                _weekly.Add(new WeeklyExerciseStats(g.Key.Exercise, g.Key.IsoYear, g.Key.IsoWeek,
                    totalSets, totalReps, totalVol, top1Rm, avgRir));
            }

            // grafik için egzersiz listesi (log’tan)
            var exList = _log.Select(x => x.Exercise)
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(s => s)
                             .ToList();
            cmbExerciseForChart.Items.Clear();
            foreach (var ex in exList) cmbExerciseForChart.Items.Add(ex);
            if (cmbExerciseForChart.Items.Count > 0 && cmbExerciseForChart.SelectedIndex < 0)
                cmbExerciseForChart.SelectedIndex = 0;

            DrawChart();
            BuildAdvice();
        }

        // ——— Grafiği seçilen METRİĞE göre çiz (güncel DrawChart) ———
        // ——— Grafiği seçilen METRİĞE göre çiz (DateTime X ekseni, ISO hafta bazlı) ———
        private void DrawChart()
        {
            chart.Series.Clear();
            if (cmbExerciseForChart.SelectedItem == null || _weekly.Count == 0) return;

            string ex = cmbExerciseForChart.SelectedItem.ToString()!;
            string metric = (cmbMetric.SelectedItem?.ToString() ?? "Hacim (kg)");

            var data = _weekly
                .Where(w => w.Exercise.Equals(ex, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.IsoYear).ThenBy(w => w.IsoWeek)
                .ToList();

            if (data.Count == 0) return;

            string seriesName;
            Func<WeeklyExerciseStats, double?> ySelector;
            string yTitle;

            switch (metric)
            {
                case "Tahmini 1RM (kg)":
                    seriesName = "Tahmini 1RM (kg)";
                    ySelector = w => w.TopSet1Rm;
                    yTitle = "1RM (kg)";
                    break;
                case "Ortalama RIR":
                    seriesName = "Ortalama RIR";
                    ySelector = w => w.AvgRIR;
                    yTitle = "RIR";
                    break;
                default:
                    seriesName = "Toplam Hacim (kg)";
                    ySelector = w => w.TotalVolume;
                    yTitle = "Hacim (kg)";
                    break;
            }

            var s = new Series(seriesName)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                XValueType = ChartValueType.DateTime
            };

            // (yıl, hafta) -> değer sözlüğü
            var dict = new Dictionary<(int Year, int Week), double?>();
            foreach (var w in data)
                dict[(w.IsoYear, w.IsoWeek)] = ySelector(w);

            // Min–Max ISO hafta aralığı
            int minYear = data.First().IsoYear, minWeek = data.First().IsoWeek;
            int maxYear = data.Last().IsoYear, maxWeek = data.Last().IsoWeek;

            // Yardımcılar
            DateTime CursorMonday(int year, int week) => ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
            (int y, int w) NextWeek((int y, int w) cur)
            {
                int weeksInYear = ISOWeek.GetWeeksInYear(cur.y);
                return cur.w < weeksInYear ? (cur.y, cur.w + 1) : (cur.y + 1, 1);
            }

            // Tüm haftaları (boş olanlar dahil) çiz
            var cur = (y: minYear, w: minWeek);
            while (cur.y < maxYear || (cur.y == maxYear && cur.w <= maxWeek))
            {
                var monday = CursorMonday(cur.y, cur.w);
                dict.TryGetValue((cur.y, cur.w), out var val);

                int idx = s.Points.AddXY(monday, val ?? double.NaN);
                string isoLabel = $"{cur.y}-W{cur.w:D2}";

                if (val.HasValue)
                {
                    s.Points[idx].Label = metric == "Ortalama RIR" ? $"{val.Value:0.0}" : $"{val.Value:0}";
                    s.Points[idx].ToolTip = $"{isoLabel}\n{seriesName}: {val.Value:0.0}";
                }
                else
                {
                    s.Points[idx].IsEmpty = true; // veri yoksa çizgide boşluk olsun
                    s.Points[idx].ToolTip = isoLabel;
                }

                cur = NextWeek(cur);
            }

            chart.Series.Add(s);

            var area = chart.ChartAreas[0];
            area.AxisX.Title = "Hafta";
            area.AxisY.Title = yTitle;

            area.AxisX.IntervalType = DateTimeIntervalType.Weeks;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Format = "dd MMM"; // haftanın pazartesisi
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Weeks;
            area.AxisX.MajorGrid.Interval = 1;
            area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;

            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;

            if (chart.Legends.Count > 0) chart.Legends[0].Enabled = false;

            area.RecalculateAxesScale();
        }
        private Trend BuildTrend(List<WeeklyExerciseStats> ordered)
        {
            var n = ordered.Count;
            var last = ordered[^1];
            var prev = n >= 2 ? ordered[^2] : last;

            double maVol = ordered.Skip(Math.Max(0, n - 3)).Average(x => x.TotalVolume);
            double maRm = ordered.Skip(Math.Max(0, n - 3)).Average(x => x.TopSet1Rm);
            double? maRir = ordered.Skip(Math.Max(0, n - 3)).Select(x => x.AvgRIR).Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty().Average();
            if (ordered.Skip(Math.Max(0, n - 3)).All(x => !x.AvgRIR.HasValue)) maRir = null;

            return new Trend
            {
                VolMA3 = maVol,
                RmMA3 = maRm,
                RirMA3 = maRir,
                VolDelta = Percent(prev.TotalVolume, last.TotalVolume),
                RmDelta = Percent(prev.TopSet1Rm, last.TopSet1Rm),
                RirDelta = (prev.AvgRIR.HasValue && last.AvgRIR.HasValue) ? last.AvgRIR - prev.AvgRIR : null
            };
        }
        private void SeedProfiles()
        {
            // Varsayılan
            var defaultUpper = new ExerciseProfile();
            var defaultLower = new ExerciseProfile { IsLower = true, UpperBodyKgStep = 2.5, LowerBodyKgStep = 5.0 };

            // Alt vücut (daha yüksek kilo adımı)
            string[] lower = { "Back Squat", "Front Squat", "Goblet Squat", "Hack Squat", "Leg Press", "Deadlift", "Romanian Deadlift", "Sumo Deadlift", "Bulgarian Split Squat", "Walking Lunge", "Reverse Lunge", "Hip Thrust", "Glute Bridge", "Leg Extension", "Leg Curl", "Seated Leg Curl", "Calf Raise", "Standing Calf Raise" };
            foreach (var ex in lower) _profiles[ex] = defaultLower;

            // Üst vücut itiş/çekiş (örnek ince ayar)
            _profiles["Bench Press"] = new ExerciseProfile { WeeklySets = (10, 18), TargetRIR = (1, 3), ProgressionPct = 0.03, IsLower = false };
            _profiles["Overhead Press"] = new ExerciseProfile { WeeklySets = (8, 14), TargetRIR = (1, 3), ProgressionPct = 0.025 };
            _profiles["Lat Pulldown"] = new ExerciseProfile { WeeklySets = (10, 18), TargetRIR = (1, 3), ProgressionPct = 0.03 };

            // Diğerleri profil yoksa default (üst)
        }
        private void BuildAdvice()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 ÖNERİLER (Progressive Overload):");
            sb.AppendLine("------------------------------------");

            var byEx = _weekly.GroupBy(w => w.Exercise);
            bool hadAny = false;

            foreach (var g in byEx)
            {
                var ordered = g.OrderBy(x => x.IsoYear).ThenBy(x => x.IsoWeek).ToList();
                if (ordered.Count < 2) continue; // en az 2 hafta

                hadAny = true;

                var trend = BuildTrend(ordered);
                var last = ordered[^1];

                // Profil getir (yoksa varsayılan üst)
                _profiles.TryGetValue(g.Key, out var p);
                p ??= new ExerciseProfile();

                // Haftalık total set = o haftaki set sayısı (Weekly stats içinde set toplamı var)
                int weekSets = last.TotalSets;

                // Hedefe göre durumlar
                bool belowSetTarget = weekSets < p.WeeklySets.Min;
                bool aboveSetTarget = weekSets > p.WeeklySets.Max;

                // RIR değerlendirmesi
                double? lastRir = last.AvgRIR;
                bool rirTooHigh = lastRir.HasValue && lastRir.Value >= p.MaxRirBeforeAddSet; // çok rahat
                bool rirOKForLoad = lastRir.HasValue && lastRir.Value >= p.MinRirForLoadIncrease; // kilo/rep artışı mümkün
                bool rirTooLow = lastRir.HasValue && lastRir.Value <= p.MinRirForSafety; // fazla zor

                // Deload tetikleyici: MA3 trende göre son 3 haftada istikrarlı ↑ ve RIR düşüşü
                bool hotStreak = trend.VolDelta > p.DeloadTriggerPct * 100 && trend.RmDelta > p.DeloadTriggerPct * 100;
                bool rirDropping = trend.RirDelta.HasValue && trend.RirDelta.Value < -0.3;
                bool suggestDeload = hotStreak && rirDropping && aboveSetTarget;

                // Plateau: düşük artış + RIR düşmüyor
                bool plateau = trend.VolDelta < 2 && trend.RmDelta < 0.5 && (!trend.RirDelta.HasValue || trend.RirDelta >= -0.2);

                // kg adımı
                double kgStep = p.IsLower ? p.LowerBodyKgStep : p.UpperBodyKgStep;

                // Öneri metni
                string rec;
                if (suggestDeload)
                {
                    rec = $"🧯 **Deload** öner: {g.Key} → setleri %{(int)(100.0 * 0.6)} seviyesine indir, RIR hedefini {Math.Max(p.TargetRIR.Min + 1, 2)} civarı tut, yükü {kgStep} kg düşür (1–2 hafta).";
                }
                else if (belowSetTarget || (plateau && rirTooHigh))
                {
                    rec = $"➕ **Set ekle**: {g.Key} → haftalık set {weekSets} (hedef {p.WeeklySets.Min}-{p.WeeklySets.Max}). Bir sonraki hafta +1 set ve aynı kiloda {p.RepsPerSet.Min}-{p.RepsPerSet.Max} aralığında çalış. RIR hedefi {p.TargetRIR.Min}-{p.TargetRIR.Max}.";
                }
                else if (plateau && rirOKForLoad)
                {
                    rec = $"⬆️ **Yük/tekrar arttır**: {g.Key} → {kgStep} kg ekle **ya da** aynı kiloda +1 tekrar/sett hedefle. RIR {lastRir:0.0} → {p.MinRirForLoadIncrease} üzeri, uygun.";
                }
                else if (rirTooLow && !belowSetTarget)
                {
                    rec = $"⚠️ **Aşırı zorlanma**: {g.Key} → bir set azalt **veya** {kgStep} kg düş. RIR {lastRir:0.0} ≤ {p.MinRirForSafety}. 1–2 hafta kontrol edip tekrar yüklen.";
                }
                else if (aboveSetTarget && (trend.VolDelta < 0 || rirDropping))
                {
                    rec = $"🧰 **Hacmi tıraşla**: {g.Key} → hedef üstündesin ({weekSets} set). -1 set yap, teknik kaliteyi artır, RIR {p.TargetRIR.Min}-{p.TargetRIR.Max}’e getir.";
                }
                else
                {
                    // Progression hedefi
                    double targetVol = trend.VolMA3 * (1.0 + p.ProgressionPct);
                    if (last.TotalVolume < targetVol * p.AddSetIfBelowPct && weekSets <= p.WeeklySets.Max - 1)
                    {
                        rec = $"📈 **Hedef hacme yaklaş**: {g.Key} → +1 set ekleyip toplam hacmi ~{targetVol:0} kg hedefle. RIR {p.TargetRIR.Min}-{p.TargetRIR.Max}.";
                    }
                    else
                    {
                        rec = $"✅ **İyi gidiyor**: {g.Key} → mevcut şemayı koru. Küçük artışlar: {kgStep} kg **veya** +1 tekrar (RIR {p.TargetRIR.Min}-{p.TargetRIR.Max}).";
                    }
                }

                sb.AppendLine(rec);
            }

            if (!hadAny)
                sb.AppendLine("Henüz karşılaştıracak yeterli veri yok. En az 2 hafta verisi gir.");

            rtbAdvice.Clear();
            rtbAdvice.AppendText(sb.ToString());
            rtbAdvice.ScrollToCaret();
        }
        private ExerciseProfile GuessProfileFor(string ex)
        {
            var lowerHints = new[] { "squat", "deadlift", "leg", "thrust", "glute", "calf", "lunge", "curl" };
            if (lowerHints.Any(h => ex.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                return new ExerciseProfile { IsLower = true };
            return new ExerciseProfile(); // üst varsayılan
        }
        private static double Percent(double prev, double curr)
        {
            if (prev <= 0 && curr <= 0) return 0;
            if (prev <= 0) return 100; // yeni başlangıç
            return (curr - prev) / prev * 100.0;
        }
    }
}
