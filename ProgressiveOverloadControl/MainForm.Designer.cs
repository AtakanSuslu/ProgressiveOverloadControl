using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace ProgressiveOverloadControl
{
    partial class MainForm
    {
        private FlowLayoutPanel topFlow;
        private DataGridView gridLog;
        private DataGridView gridWeekly;
        private SplitContainer rightSplit;
        private Chart chart;
        private RichTextBox rtbAdvice;
        private DateTimePicker dtpDate;
        private TextBox txtSet, txtRep, txtKg, txtRir;
        private Button btnAdd, btnDelete, btnRecalc;
        private ComboBox cmbExerciseForChart;
        private ComboBox cmbMetric;
        private ComboBox cmbExInput;

        private void InitializeComponent()
        {
            this.topFlow = new FlowLayoutPanel();
            this.dtpDate = new DateTimePicker();
            this.txtSet = new TextBox();
            this.txtRep = new TextBox();
            this.txtKg = new TextBox();
            this.txtRir = new TextBox();
            this.btnAdd = new Button();
            this.btnDelete = new Button();
            this.btnRecalc = new Button();
            this.cmbExerciseForChart = new ComboBox();

            this.gridLog = new DataGridView();
            this.gridWeekly = new DataGridView();
            this.rightSplit = new SplitContainer();
            this.chart = new Chart();
            this.rtbAdvice = new RichTextBox();

            // ---- Form ----
            this.SuspendLayout();
            this.Text = "Progressive Overload Tracker";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Dpi;      // DPI uyumlu
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.MinimumSize = new Size(1000, 600);
            this.Width = 1200;
            this.Height = 720;




            // ---- Top Flow (üst bar) ----
            this.topFlow.Dock = DockStyle.Top;
            this.topFlow.Height = 72;
            this.topFlow.Padding = new Padding(8);
            this.topFlow.AutoScroll = true;
            this.topFlow.WrapContents = false;          // tek satırda kaydır
            this.topFlow.FlowDirection = FlowDirection.LeftToRight;

            // Ortak margin helper
            var m = new Padding(8, 8, 0, 0);
            var m2 = new Padding(0, 8, 0, 0);
            // Inputlar

            // ---- METRİK SEÇİMİ (YENİ) ----
            var lblMetric = new Label { Text = "Metri̇k", AutoSize = true, Margin = m };
            this.cmbMetric = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Margin = m };
            this.cmbMetric.Items.AddRange(new object[] { "Hacim (kg)", "Tahmini 1RM (kg)", "Ortalama RIR" });
            this.cmbMetric.SelectedIndex = 0; // varsayılan: Hacim

            var lblTarih = new Label { Text = "Tarih", AutoSize = true, Margin = m2 };
            this.dtpDate.Width = 140; this.dtpDate.Margin = m;

            var lblEx = new Label { Text = "Egzersiz", AutoSize = true, Margin = m };
            cmbExInput = new ComboBox
            {
                Width = 200,
                Margin = m,
                DropDownStyle = ComboBoxStyle.DropDown,       // yaz + seç
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            var lblSet = new Label { Text = "Set", AutoSize = true, Margin = m };
            this.txtSet.Width = 50; this.txtSet.Margin = m; this.txtSet.PlaceholderText = "Set";

            var lblRep = new Label { Text = "Tek.", AutoSize = true, Margin = m };
            this.txtRep.Width = 60; this.txtRep.Margin = m; this.txtRep.PlaceholderText = "Tek.";

            var lblKg = new Label { Text = "Kg", AutoSize = true, Margin = m };
            this.txtKg.Width = 70; this.txtKg.Margin = m; this.txtKg.PlaceholderText = "Kg";

            var lblRir = new Label { Text = "RIR", AutoSize = true, Margin = m };
            this.txtRir.Width = 60; this.txtRir.Margin = m; this.txtRir.PlaceholderText = "RIR (ops.)";

            this.btnAdd.Text = "Ekle"; this.btnAdd.Width = 70; this.btnAdd.Margin = m;
            this.btnDelete.Text = "Seçili Sil"; this.btnDelete.Width = 100; this.btnDelete.Margin = m;
            this.btnRecalc.Text = "Yeniden Hesap"; this.btnRecalc.Width = 130; this.btnRecalc.Margin = m;

            var lblChartSel = new Label { Text = "Grafik Egzersiz", AutoSize = true, Margin = m };
            this.cmbExerciseForChart.Width = 180; this.cmbExerciseForChart.DropDownStyle = ComboBoxStyle.DropDownList; this.cmbExerciseForChart.Margin = m;

            this.topFlow.Controls.AddRange(new Control[] {
    lblTarih, dtpDate,

    // Egzersiz alanı doğru sırayla
    lblEx, cmbExInput,

    lblSet, txtSet,
    lblRep, txtRep,
    lblKg, txtKg,
    lblRir, txtRir,

    btnAdd, btnDelete, btnRecalc,
    lblChartSel, cmbExerciseForChart,

    // Metrik label'ını eklemeyi unutma
    lblMetric, cmbMetric
});

            // ---- Grid Log (sol) ----
            this.gridLog.Dock = DockStyle.Left;
            this.gridLog.Width = 560;
            this.gridLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridLog.ReadOnly = true;
            this.gridLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.gridLog.MultiSelect = true;
            this.gridLog.RowHeadersVisible = false;

            // ---- Grid Weekly (orta) ----
            this.gridWeekly.Dock = DockStyle.Fill;
            this.gridWeekly.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridWeekly.ReadOnly = true;
            this.gridWeekly.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.gridWeekly.RowHeadersVisible = false;

            // ---- Right Split (sağ) ----
            this.rightSplit.Dock = DockStyle.Right;
            this.rightSplit.Width = 420;
            this.rightSplit.Orientation = Orientation.Horizontal;
            this.rightSplit.SplitterDistance = 320;
            this.rightSplit.IsSplitterFixed = false; // kullanıcı ayarlayabilir

            // Chart
            this.chart.Dock = DockStyle.Fill;
            this.chart.ChartAreas.Add(new ChartArea("area"));
            this.chart.Legends.Add(new Legend("legend"));

            // Advice
            this.rtbAdvice.Dock = DockStyle.Fill;
            this.rtbAdvice.ReadOnly = true;

            this.rightSplit.Panel1.Controls.Add(this.chart);
            this.rightSplit.Panel2.Controls.Add(this.rtbAdvice);

            // ---- Form Controls (sıra önemli: önce top, sonra sol, sonra sağ, en son fill) ----
            this.Controls.Add(this.gridWeekly);
            this.Controls.Add(this.gridLog);
            this.Controls.Add(this.rightSplit);
            this.Controls.Add(this.topFlow);

            this.ResumeLayout(false);
        }
    }
}
