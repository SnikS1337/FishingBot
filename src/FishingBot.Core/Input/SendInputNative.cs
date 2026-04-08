using System.Runtime.InteropServices;

namespace FishingBot.Core.Input;

internal static class SendInputNative
{
    private const int InputKeyboard = 1;
    private const int InputMouse = 0;

    private const uint KeyEventfScancode = 0x0008;
    private const uint KeyEventfKeyup = 0x0002;

    private const uint MouseeventfLeftdown = 0x0002;
    private const uint MouseeventfLeftup = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetCursorPos(int x, int y);

    internal static void KeyDown(ushort scanCode)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = scanCode,
                        dwFlags = KeyEventfScancode
                    }
                }
            }
        };

        _ = SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    internal static void KeyUp(ushort scanCode)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = scanCode,
                        dwFlags = KeyEventfScancode | KeyEventfKeyup
                    }
                }
            }
        };

        _ = SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    internal static void PressKey(ushort scanCode)
    {
        KeyDown(scanCode);
        KeyUp(scanCode);
    }

    internal static void LeftClick()
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MouseeventfLeftdown
                    }
                }
            },
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MouseeventfLeftup
                    }
                }
            }
        };

        _ = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }
}
