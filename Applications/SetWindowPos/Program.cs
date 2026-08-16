using System;
using System.Diagnostics;
using System.Drawing;
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
            // Runs as a tray app: checks every second for the process's window and,
            // the moment it appears, positions it and shows a tray notification.
            // Keeps watching afterward so it repositions again if the window closes
            // and reopens (e.g. the game is restarted).

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

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var context = new WindowWatcherContext(settings))
            {
                Application.Run(context);
            }

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
                "Once running, the app sits in the system tray and checks every second for\n" +
                "the process's window, positioning it and notifying you the moment it appears.";
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

    internal sealed class WindowWatcherContext : ApplicationContext
    {
        private const int PollIntervalMs = 1000;

        private readonly MonitorSettings _settings;
        private readonly NotifyIcon _trayIcon;
        private readonly Icon _watchingIcon;
        private readonly Icon _foundIcon;
        private readonly System.Windows.Forms.Timer _timer;
        private bool _isPositioned;

        public WindowWatcherContext(MonitorSettings settings)
        {
            _settings = settings;
            _watchingIcon = CreateDotIcon(Color.Gray);
            _foundIcon = CreateDotIcon(Color.LimeGreen);

            var menu = new ContextMenuStrip();
            menu.Items.Add($"Watching for '{_settings.ProcessName}'").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitApplication());

            _trayIcon = new NotifyIcon
            {
                Icon = _watchingIcon,
                Text = Truncate($"Watching for '{_settings.ProcessName}'..."),
                ContextMenuStrip = menu,
                Visible = true
            };

            _timer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
            _timer.Tick += (_, _) => CheckWindow();

            if (_settings.WaitSeconds > 0)
            {
                var startupDelay = new System.Windows.Forms.Timer { Interval = _settings.WaitSeconds * 1000 };
                startupDelay.Tick += (_, _) =>
                {
                    startupDelay.Stop();
                    startupDelay.Dispose();
                    _timer.Start();
                };
                startupDelay.Start();
            }
            else
            {
                _timer.Start();
            }
        }

        private void CheckWindow()
        {
            IntPtr handle = WindowFinder.FindMainWindowHandle(_settings.ProcessName);

            if (handle != IntPtr.Zero)
            {
                if (!_isPositioned)
                {
                    WindowFinder.PositionWindow(handle, _settings);
                    _isPositioned = true;
                    _trayIcon.Icon = _foundIcon;
                    _trayIcon.Text = Truncate($"'{_settings.ProcessName}' found and positioned.");
                    _trayIcon.BalloonTipTitle = "Window found";
                    _trayIcon.BalloonTipText = $"'{_settings.ProcessName}' appeared and was moved/resized.";
                    _trayIcon.ShowBalloonTip(3000);
                }
            }
            else if (_isPositioned)
            {
                _isPositioned = false;
                _trayIcon.Icon = _watchingIcon;
                _trayIcon.Text = Truncate($"Watching for '{_settings.ProcessName}'...");
            }
        }

        private void ExitApplication()
        {
            _timer.Stop();
            _trayIcon.Visible = false;
            ExitThread();
        }

        private static string Truncate(string text)
        {
            // NotifyIcon.Text is limited to 127 characters.
            return text.Length <= 127 ? text : text.Substring(0, 127);
        }

        private static Icon CreateDotIcon(Color color)
        {
            using var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using var brush = new SolidBrush(color);
                g.FillEllipse(brush, 1, 1, 14, 14);
            }

            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _trayIcon.Dispose();
                _watchingIcon.Dispose();
                _foundIcon.Dispose();
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
