using System.Runtime.InteropServices;

namespace UltraPaste;

internal static class NativeMethods
{
    // Hotkey
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Window management
    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern bool AllowSetForegroundWindow(int dwProcessId);

    // Cursor
    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    // SendInput
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    // On x64 Windows: INPUT is 40 bytes (4 type + 4 pad + 32 union)
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    internal struct INPUT
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public KEYBDINPUT Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const ushort VK_CONTROL = 0x11;
    internal const ushort VK_V = 0x56;

    internal static void SendCtrlV()
    {
        INPUT[] inputs =
        [
            new INPUT { Type = INPUT_KEYBOARD, Ki = new KEYBDINPUT { Vk = VK_CONTROL } },
            new INPUT { Type = INPUT_KEYBOARD, Ki = new KEYBDINPUT { Vk = VK_V } },
            new INPUT { Type = INPUT_KEYBOARD, Ki = new KEYBDINPUT { Vk = VK_V, Flags = KEYEVENTF_KEYUP } },
            new INPUT { Type = INPUT_KEYBOARD, Ki = new KEYBDINPUT { Vk = VK_CONTROL, Flags = KEYEVENTF_KEYUP } },
        ];
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
