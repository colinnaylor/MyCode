using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SetWindowPosition
{
    internal static class Program
    {
        // ----- Win32 interop -----

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
        private const int GWL_EXSTYLE = -20;

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

        // ----- Entry point -----

        private static int Main(string[] args)
        {
            // Usage:
            //   SetWindowPosition.exe <processName> <x> <y> <width> <height> [--wait-seconds N] [--borderless] [--retries N]
            //
            // Example:
            //   SetWindowPosition.exe eurotrucks2 -1080 0 6000 1920 --wait-seconds 15
            // args = [ "eurotrucks2", "-1080", "0", "6000", "1920", "--wait- seconds 15"];

            if (args.Length < 5)
            {
                PrintUsage();
                return 1;
            }

            string processName = args[0];

            if (!int.TryParse(args[1], out int x) ||
                !int.TryParse(args[2], out int y) ||
                !int.TryParse(args[3], out int width) ||
                !int.TryParse(args[4], out int height))
            {
                Console.WriteLine("Error: x, y, width and height must all be integers.");
                PrintUsage();
                return 1;
            }

            int waitSeconds = GetIntOption(args, "--wait-seconds", 0);
            int retries = GetIntOption(args, "--retries", 20);
            bool borderless = HasFlag(args, "--borderless");
            int retryDelayMs = GetIntOption(args, "--retry-delay-ms", 1000);

            if (waitSeconds > 0)
            {
                Console.WriteLine($"Waiting {waitSeconds}s before searching for '{processName}'...");
                Thread.Sleep(waitSeconds * 1000);
            }

            IntPtr handle = IntPtr.Zero;

            for (int attempt = 1; attempt <= retries; attempt++)
            {
                handle = FindMainWindowHandle(processName);
                if (handle != IntPtr.Zero)
                {
                    break;
                }

                Console.WriteLine($"[{attempt}/{retries}] '{processName}' window not found yet, retrying in {retryDelayMs}ms...");
                Thread.Sleep(retryDelayMs);
            }

            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"Error: could not find a visible top-level window for process '{processName}'.");
                return 2;
            }

            Console.WriteLine($"Found window handle 0x{handle.ToInt64():X} for '{processName}'.");

            // Make sure it's not minimized/maximized before we try to move/resize it.
            ShowWindow(handle, SW_RESTORE);
            ShowWindow(handle, SW_SHOWNORMAL);

            if (borderless)
            {
                RemoveBorder(handle);
            }

            bool ok = SetWindowPos(
                handle,
                HWND_TOP,
                x,
                y,
                width,
                height,
                SWP_NOZORDER | SWP_SHOWWINDOW | SWP_FRAMECHANGED);

            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                Console.WriteLine($"SetWindowPos failed. Win32 error code: {err}");
                return 3;
            }

            Console.WriteLine($"Window moved/resized to X={x}, Y={y}, Width={width}, Height={height}.");
            return 0;
        }

        // ----- Helpers -----

        private static void RemoveBorder(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME);
            SetWindowLong32(hWnd, GWL_STYLE, style);
        }

        private static IntPtr FindMainWindowHandle(string processName)
        {
            // Strip a trailing ".exe" if the user included it.
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SetWindowPosition.exe <processName> <x> <y> <width> <height> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --wait-seconds N     Wait N seconds before searching (lets a slow-launching game finish loading). Default 0.");
            Console.WriteLine("  --retries N          Number of times to retry finding the window if not found immediately. Default 20.");
            Console.WriteLine("  --retry-delay-ms N   Milliseconds to wait between retries. Default 1000.");
            Console.WriteLine("  --borderless         Strip the title bar/border from the window before positioning it.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  SetWindowPosition.exe eurotrucks2 -1080 0 6000 1920 --wait-seconds 15");
        }
    }
}
