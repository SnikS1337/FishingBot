using System.Windows;
using System.Windows.Interop;
using FishingBot.App.Services;
using FishingBot.App.ViewModels;
using System.Runtime.InteropServices;
using System;

namespace FishingBot.App;

public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyPauseId = 9001;
    private const int HotkeyPanicId = 9002;

    private const uint ModNone = 0;
    private const uint VkF9 = 0x78;
    private const uint VkF10 = 0x79;

    private readonly MainViewModel _viewModel;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new UiDispatcher(Dispatcher));
        DataContext = _viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WndProc);

        RegisterGlobalHotkeys();
    }

    protected override void OnClosed(EventArgs e)
    {
        UnregisterGlobalHotkeys();

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
        }

        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void RegisterGlobalHotkeys()
    {
        var helper = new WindowInteropHelper(this);
        _ = RegisterHotKey(helper.Handle, HotkeyPauseId, ModNone, VkF9);
        _ = RegisterHotKey(helper.Handle, HotkeyPanicId, ModNone, VkF10);
    }

    private void UnregisterGlobalHotkeys()
    {
        var helper = new WindowInteropHelper(this);
        _ = UnregisterHotKey(helper.Handle, HotkeyPauseId);
        _ = UnregisterHotKey(helper.Handle, HotkeyPanicId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        var hotkeyId = wParam.ToInt32();
        if (hotkeyId == HotkeyPauseId)
        {
            _viewModel.TriggerPauseResumeFromHotkey();
            handled = true;
        }
        else if (hotkeyId == HotkeyPanicId)
        {
            _viewModel.TriggerPanicStopFromHotkey();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
