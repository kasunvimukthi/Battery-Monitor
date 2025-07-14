using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
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

        public Form1()
        {
            InitializeComponent();

            if (!Properties.Settings.Default.ClosePermanenly)
            {
                AddToStartup();
            }

            int x = Properties.Settings.Default.WidgetX;
            int y = Properties.Settings.Default.WidgetY;
            
            // Widget look
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(x, y);
            this.Size = new Size(220, 100);
            this.BackColor = Color.Black;
            this.TopMost = false; // Make it float behind active windows
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;


            

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

            // Close button (hover only)
            closeButton = new Button
            {
                Text = "X",
                Size = new Size(25, 25),
                Location = new Point(this.Width - 30, 5),
                //BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = true
            };
            this.Controls.Add(closeButton);

            closeButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("Close the battery widget?", "Confirm Exit", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                    // Mark widget as permanently closed
                    Properties.Settings.Default.ClosePermanenly= true;
                    Properties.Settings.Default.Save();
                    Application.Exit();

                    this.Close();
            };

            // Notify icon
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true
            };
            this.FormClosing += (s, e) => notifyIcon.Dispose();

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
            float batteryPercent = powerStatus.BatteryLifePercent * 100;
            bool isCharging = powerStatus.PowerLineStatus == PowerLineStatus.Online;
            int batterySeconds = powerStatus.BatteryLifeRemaining;

            batteryLabel.Text = $"Battery: {batteryPercent:F0}%";
            batteryProgressBar.Value = Math.Max(1, (int)batteryPercent);

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

            // Low battery notification
            if (!isCharging && batteryPercent < 20)
            {
                notifyIcon.BalloonTipTitle = "Low Battery Warning";
                notifyIcon.BalloonTipText = "Battery is below 20%. Please plug in your charger.";
                notifyIcon.ShowBalloonTip(3000);
            }
        }

        // Enable dragging from anywhere on the form
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HT_CAPTION = 0x2;
            const int WM_NCLBUTTONDBLCLK = 0xA3; // Double-click on non-client area

            // Disable maximize on double-click
            if (m.Msg == WM_NCLBUTTONDBLCLK)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Enable drag
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
