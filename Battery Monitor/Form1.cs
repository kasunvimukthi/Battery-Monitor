using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IWshRuntimeLibrary; // Add COM reference: Windows Script Host Object Model

namespace Battery_Monitor
{
    public partial class Form1 : Form
    {
        private Label batteryLabel;
        private Label chargingStatusLabel;
        private Label timeRemainingLabel;
        private ProgressBar batteryProgressBar;
        private System.Windows.Forms.Timer updateTimer;
        private NotifyIcon notifyIcon;
        private Button closeButton;
        private Point dragOffset;

        private Icon _currentTrayIcon;
        private int _lastIconPercent = -1;
        private bool _lastIconCharging = false;
        private bool _lowBatteryBalloonShown = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public Form1()
        {
            InitializeComponent();

            if (!Properties.Settings.Default.ClosePermanenly)
                AddToStartup();

            int x = Properties.Settings.Default.WidgetX;
            int y = Properties.Settings.Default.WidgetY;

            // Widget look
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(x, y);
            this.Size = new Size(220, 100);
            this.BackColor = Color.Black;
            this.ShowInTaskbar = false;

            // Battery %
            batteryLabel = new Label
            {
                Text = "Battery: --%",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.LimeGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(batteryLabel);

            // Charging status
            chargingStatusLabel = new Label
            {
                Text = "Charging Status: --",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(chargingStatusLabel);

            // Time remaining
            timeRemainingLabel = new Label
            {
                Text = "Time Left: --",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(timeRemainingLabel);

            // Progress bar
            batteryProgressBar = new ProgressBar
            {
                Location = new Point(10, 75),
                Width = 200,
                Height = 15
            };
            this.Controls.Add(batteryProgressBar);

            // Close button
            closeButton = new Button
            {
                Text = "X",
                Size = new Size(25, 25),
                Location = new Point(this.Width - 30, 5),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = true
            };
            this.Controls.Add(closeButton);

            closeButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("Close the battery widget?", "Confirm Exit", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Properties.Settings.Default.ClosePermanenly = true;
                    Properties.Settings.Default.Save();
                    ExitApp();
                }
            };

            // Notify icon
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true
            };
            this.FormClosing += (s, e) => notifyIcon.Dispose();

            SetupTrayMenu();

            // Battery update timer
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 5000;
            updateTimer.Tick += UpdateBatteryInfo;
            updateTimer.Start();

            UpdateBatteryInfo(null, null);
        }

        private void UpdateBatteryInfo(object sender, EventArgs e)
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            float batteryPercent = powerStatus.BatteryLifePercent * 100f;
            bool isCharging = powerStatus.PowerLineStatus == PowerLineStatus.Online;
            int batterySeconds = powerStatus.BatteryLifeRemaining;

            int pctInt = Math.Max(0, Math.Min(100, (int)Math.Round(batteryPercent)));

            batteryLabel.Text = $"Battery: {pctInt}%";
            batteryProgressBar.Value = Math.Max(1, pctInt);
            chargingStatusLabel.Text = isCharging ? "Charging" : "Not Charging";

            if (batterySeconds > 0)
            {
                TimeSpan remaining = TimeSpan.FromSeconds(batterySeconds);
                timeRemainingLabel.Text = $"Time Left: {remaining.Hours:D2}h {remaining.Minutes:D2}min";
            }
            else
            {
                timeRemainingLabel.Text = "Time Left: Unknown";
            }

            // Low battery balloon logic
            if (!isCharging && pctInt < 20)
            {
                if (!_lowBatteryBalloonShown)
                {
                    notifyIcon.BalloonTipTitle = "Low Battery Warning";
                    notifyIcon.BalloonTipText = "Battery is below 20%. Please plug in your charger.";
                    notifyIcon.ShowBalloonTip(3000);
                    _lowBatteryBalloonShown = true;
                }
            }
            else
            {
                if (_lowBatteryBalloonShown && (isCharging || pctInt >= 25))
                {
                    HideBalloonTip();
                }
            }

            UpdateTrayIcon(pctInt, isCharging);
        }

        private void HideBalloonTip()
        {
            notifyIcon.Visible = false;
            notifyIcon.Visible = true;

            if (_currentTrayIcon != null)
                notifyIcon.Icon = _currentTrayIcon;
            notifyIcon.Text = $"{_lastIconPercent}% - {(_lastIconCharging ? "Charging" : "On battery")}";

            _lowBatteryBalloonShown = false;
        }

        // ✅ New large % circle icon style
        private void UpdateTrayIcon(int percent, bool isCharging)
        {
            if (percent == _lastIconPercent && isCharging == _lastIconCharging)
                return;

            _lastIconPercent = percent;
            _lastIconCharging = isCharging;

            int size = 64; // render large for better scaling
            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                // Background color based on state
                Color bgColor = isCharging ? Color.DodgerBlue :
                                percent <= 20 ? Color.Red :
                                percent <= 50 ? Color.Gold :
                                Color.LimeGreen;

                using (Brush bgBrush = new SolidBrush(bgColor))
                    g.FillEllipse(bgBrush, 0, 0, size, size);

                // Draw large percentage text
                string text = percent.ToString();
                float fontSize = text.Length >= 3 ? 26f : 30f;
                using (Font f = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush tb = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(text, f, tb, new RectangleF(0, 0, size, size), sf);
                }

                IntPtr hIcon = bmp.GetHicon();
                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    Icon clone = (Icon)temp.Clone();
                    notifyIcon.Icon = clone;

                    _currentTrayIcon?.Dispose();
                    _currentTrayIcon = clone;
                }
                DestroyIcon(hIcon);
            }

            notifyIcon.Text = $"{percent}% - {(isCharging ? "Charging" : "On battery")}";
        }

        private void SetupTrayMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show Widget", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            menu.Items.Add("Hide Widget", null, (s, e) => { this.Hide(); });
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            notifyIcon.ContextMenuStrip = menu;

            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (this.Visible) this.Hide();
                    else this.Show();
                }
            };
        }

        private void ExitApp()
        {
            Properties.Settings.Default.ClosePermanenly = true;
            Properties.Settings.Default.Save();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HT_CAPTION = 0x2;
            const int WM_NCLBUTTONDBLCLK = 0xA3;

            if (m.Msg == WM_NCLBUTTONDBLCLK)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HT_CAPTION;
                return;
            }

            base.WndProc(ref m);

            Properties.Settings.Default.WidgetX = this.Location.X;
            Properties.Settings.Default.WidgetY = this.Location.Y;
            Properties.Settings.Default.Save();
        }

        private void AddToStartup()
        {
            string exePath = Application.ExecutablePath;
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "BatteryMonitor.lnk");

            if (!System.IO.File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Application.StartupPath;
                shortcut.WindowStyle = 1;
                shortcut.Description = "Battery Monitor Widget";
                shortcut.Save();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.EnableVisualStyles();
        }
    }
}
