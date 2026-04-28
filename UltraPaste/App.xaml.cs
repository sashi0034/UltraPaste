using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace UltraPaste;

public partial class App : Application
{
    private const int HotkeyId = 9001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint VK_V = 0x56;
    private const int WM_HOTKEY = 0x0312;

    private NotifyIcon? _trayIcon;
    private HwndSource? _hwndSource;
    private MainWindow? _popup;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        SetupTrayIcon();
        SetupHotkey();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Visible = true,
            Text = "UltraPaste (Ctrl+Alt+Shift+V)"
        };

        var startItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = StartupManager.IsEnabled()
        };
        startItem.Click += (_, _) =>
        {
            if (StartupManager.IsEnabled())
            {
                StartupManager.Disable();
                startItem.Checked = false;
            }
            else
            {
                StartupManager.Enable();
                startItem.Checked = true;
            }
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(startItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowPopup);
    }

    private void SetupHotkey()
    {
        var srcParams = new HwndSourceParameters("UltraPaste_Hotkey")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
            PositionX = -32000,
            PositionY = -32000,
        };
        _hwndSource = new HwndSource(srcParams);
        _hwndSource.AddHook(WndProc);

        if (!NativeMethods.RegisterHotKey(_hwndSource.Handle, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_SHIFT, VK_V))
        {
            MessageBox.Show(
                "Failed to register hotkey Ctrl+Alt+Shift+V.\nIt may already be in use by another application.",
                "UltraPaste",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Dispatcher.Invoke(TogglePopup);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void TogglePopup()
    {
        if (_popup is { IsVisible: true })
            _popup.HidePopup();
        else
            ShowPopup();
    }

    private void ShowPopup()
    {
        if (_popup == null || !_popup.IsLoaded)
            _popup = new MainWindow();

        _popup.ShowAtCursor();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hwndSource != null)
        {
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            _hwndSource.Dispose();
        }
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
