using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SetWindowPosition
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // Usage:
            //   SetWindowPosition.exe <processName> <x> <y> <width> <height> [--wait-seconds N] [--borderless]
            //
            // Example:
            //   SetWindowPosition.exe eurotrucks2 -1080 0 6000 1920 --wait-seconds 15
            //
            // Opens a normal window (Alt-Tab visible) that shows the parameters
            // you passed in and checks every second for the process's window.
            // The moment it appears, it's positioned and the window shows
            // that it was found. Keeps watching afterward so it repositions
            // again if the window closes and reopens (e.g. the game is restarted).

            if (args.Length < 5)
            {
                MessageBox.Show(UsageText(), "SetWindowPosition", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 1;
            }

            string processName = args[0];

            if (!int.TryParse(args[1], out int x) ||
                !int.TryParse(args[2], out int y) ||
                !int.TryParse(args[3], out int width) ||
                !int.TryParse(args[4], out int height))
            {
                MessageBox.Show("Error: x, y, width and height must all be integers.\n\n" + UsageText(), "SetWindowPosition", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            int waitSeconds = GetIntOption(args, "--wait-seconds", 0);
            bool borderless = HasFlag(args, "--borderless");

            var settings = new MonitorSettings(processName, x, y, width, height, borderless, waitSeconds);

            // Stay DPI-unaware so Windows scales our SetWindowPos coordinates the
            // same way it did for the original console version — otherwise the
            // X/Y/width/height values you're used to no longer land in the same
            // physical place on a scaled display.
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm(settings));

            return 0;
        }

        private static int GetIntOption(string[] args, string optionName, int defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(args[i + 1], out int value))
                    {
                        return value;
                    }
                }
            }

            return defaultValue;
        }

        private static bool HasFlag(string[] args, string flagName)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, flagName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string UsageText()
        {
            return
                "Usage:\n" +
                "  SetWindowPosition.exe <processName> <x> <y> <width> <height> [options]\n\n" +
                "Options:\n" +
                "  --wait-seconds N   Wait N seconds before starting to watch for the window. Default 0.\n" +
                "  --borderless       Strip the title bar/border from the window before positioning it.\n\n" +
                "Example:\n" +
                "  SetWindowPosition.exe eurotrucks2 -1080 0 6000 1920 --wait-seconds 15\n\n" +
                "Once running, the app window shows the parameters you passed in and checks\n" +
                "every second for the process's window, positioning it the moment it appears.";
        }
    }

    internal sealed class MonitorSettings
    {
        public string ProcessName { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Borderless { get; }
        public int WaitSeconds { get; }

        public MonitorSettings(string processName, int x, int y, int width, int height, bool borderless, int waitSeconds)
        {
            ProcessName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName.Substring(0, processName.Length - 4)
                : processName;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Borderless = borderless;
            WaitSeconds = waitSeconds;
        }
    }

    internal sealed class MainForm : Form
    {
        private const int PollIntervalMs = 1000;

        private readonly System.Windows.Forms.Timer _timer;
        private readonly Icon _appIcon;
        private readonly Label _statusLabel;
        private readonly Label _lastCheckedLabel;
        private readonly Panel _statusPanel;

        private readonly TextBox _processNameBox;
        private readonly NumericUpDown _xBox;
        private readonly NumericUpDown _yBox;
        private readonly NumericUpDown _widthBox;
        private readonly NumericUpDown _heightBox;
        private readonly CheckBox _borderlessBox;
        private readonly NumericUpDown _waitSecondsBox;

        private MonitorSettings _settings;
        private bool _isPositioned;

        public MainForm(MonitorSettings settings)
        {
            _settings = settings;
            _appIcon = CreateAppIcon();

            Text = "Set Window Pos";
            Icon = _appIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 340);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _processNameBox = new TextBox { Text = settings.ProcessName, Width = 220 };
            _xBox = CreateNumericBox(settings.X);
            _yBox = CreateNumericBox(settings.Y);
            _widthBox = CreateNumericBox(settings.Width, min: 1);
            _heightBox = CreateNumericBox(settings.Height, min: 1);
            _borderlessBox = new CheckBox { Checked = settings.Borderless };
            _waitSecondsBox = CreateNumericBox(settings.WaitSeconds, min: 0, max: 600);

            AddRow(layout, "Process name:", _processNameBox);
            AddRow(layout, "X:", _xBox);
            AddRow(layout, "Y:", _yBox);
            AddRow(layout, "Width:", _widthBox);
            AddRow(layout, "Height:", _heightBox);
            AddRow(layout, "Borderless:", _borderlessBox);
            AddRow(layout, "Wait seconds:", _waitSecondsBox);

            var applyButton = new Button { Text = "Apply", AutoSize = true };
            applyButton.Click += (_, _) => ApplySettings();
            AddRow(layout, string.Empty, applyButton);

            Controls.Add(layout);

            _statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Gainsboro
            };

            _statusLabel = new Label
            {
                Text = $"Watching for '{settings.ProcessName}'...",
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            _lastCheckedLabel = new Label
            {
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 24
            };

            _statusPanel.Controls.Add(_lastCheckedLabel);
            _statusPanel.Controls.Add(_statusLabel);
            Controls.Add(_statusPanel);

            _timer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
            _timer.Tick += (_, _) => CheckWindow();

            Load += (_, _) => StartWatching(_settings.WaitSeconds);
        }

        private static NumericUpDown CreateNumericBox(int value, int min = -20000, int max = 20000)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, value)),
                Width = 100
            };
        }

        private void ApplySettings()
        {
            string processName = _processNameBox.Text.Trim();
            if (processName.Length == 0)
            {
                MessageBox.Show(this, "Process name cannot be empty.", "Set Window Pos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings = new MonitorSettings(
                processName,
                (int)_xBox.Value,
                (int)_yBox.Value,
                (int)_widthBox.Value,
                (int)_heightBox.Value,
                _borderlessBox.Checked,
                (int)_waitSecondsBox.Value);

            _isPositioned = false;
            _timer.Stop();
            StartWatching(_settings.WaitSeconds);
        }

        private void StartWatching(int waitSeconds)
        {
            if (waitSeconds > 0)
            {
                _statusPanel.BackColor = Color.Gainsboro;
                _statusLabel.Text = $"Waiting {waitSeconds}s before watching for '{_settings.ProcessName}'...";
                var startupDelay = new System.Windows.Forms.Timer { Interval = waitSeconds * 1000 };
                startupDelay.Tick += (_, _) =>
                {
                    startupDelay.Stop();
                    startupDelay.Dispose();
                    _statusLabel.Text = $"Watching for '{_settings.ProcessName}'...";
                    _timer.Start();
                };
                startupDelay.Start();
            }
            else
            {
                _statusPanel.BackColor = Color.Gainsboro;
                _statusLabel.Text = $"Watching for '{_settings.ProcessName}'...";
                _timer.Start();
            }
        }

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            int row = layout.RowCount;
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = label, Font = new Font(Control.DefaultFont, FontStyle.Bold), AutoSize = true, Margin = new Padding(3, 6, 12, 3) }, 0, row);
            control.Margin = new Padding(3);
            layout.Controls.Add(control, 1, row);
        }

        private void CheckWindow()
        {
            IntPtr handle = WindowFinder.FindMainWindowHandle(_settings.ProcessName);
            _lastCheckedLabel.Text = $"Last checked: {DateTime.Now:HH:mm:ss}";

            if (handle != IntPtr.Zero)
            {
                if (!_isPositioned)
                {
                    WindowFinder.PositionWindow(handle, _settings);
                    _isPositioned = true;
                    _statusPanel.BackColor = Color.LightGreen;
                    _statusLabel.Text = $"Found and positioned at {DateTime.Now:HH:mm:ss}";
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            else if (_isPositioned)
            {
                _isPositioned = false;
                _statusPanel.BackColor = Color.Gainsboro;
                _statusLabel.Text = $"Watching for '{_settings.ProcessName}'...";
            }
        }

        private static Icon CreateAppIcon()
        {
            const int size = 64;
            using var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var bgPath = RoundedRect(new Rectangle(2, 2, size - 4, size - 4), 14))
                using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.FromArgb(255, 0, 99, 177), Color.FromArgb(255, 0, 172, 193), 45f))
                {
                    g.FillPath(bgBrush, bgPath);
                }

                using (var whitePen = new Pen(Color.White, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    int cx = size / 2;
                    int cy = size / 2;
                    const int r = 14;
                    const int tick = 8;

                    g.DrawEllipse(whitePen, cx - r, cy - r, r * 2, r * 2);
                    using (var dotBrush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(dotBrush, cx - 4, cy - 4, 8, 8);
                    }

                    g.DrawLine(whitePen, cx, cy - r - tick, cx, cy - r + 4);
                    g.DrawLine(whitePen, cx, cy + r - 4, cx, cy + r + tick);
                    g.DrawLine(whitePen, cx - r - tick, cy, cx - r + 4, cy);
                    g.DrawLine(whitePen, cx + r - 4, cy, cx + r + tick, cy);
                }
            }

            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _appIcon.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal static class WindowFinder
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int GWL_STYLE = -16;

        private const int SW_RESTORE = 9;
        private const int SW_SHOWNORMAL = 1;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        // Common style bits, in case you want to strip the border/title bar
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;

        public static IntPtr FindMainWindowHandle(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process proc in processes)
            {
                IntPtr mainHandle = proc.MainWindowHandle;

                if (mainHandle != IntPtr.Zero &&
                    IsWindowVisible(mainHandle) &&
                    GetWindowTextLength(mainHandle) > 0)
                {
                    return mainHandle;
                }
            }

            return IntPtr.Zero;
        }

        public static void PositionWindow(IntPtr handle, MonitorSettings settings)
        {
            // Make sure it's not minimized/maximized before we try to move/resize it.
            ShowWindow(handle, SW_RESTORE);
            ShowWindow(handle, SW_SHOWNORMAL);

            if (settings.Borderless)
            {
                RemoveBorder(handle);
            }

            SetWindowPos(
                handle,
                HWND_TOP,
                settings.X,
                settings.Y,
                settings.Width,
                settings.Height,
                SWP_NOZORDER | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        }

        private static void RemoveBorder(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME);
            SetWindowLong32(hWnd, GWL_STYLE, style);
        }
    }
}
