using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls;
using FishingBot.App.Services;
using FishingBot.App.ViewModels;
using System.Runtime.InteropServices;
using System;
using System.ComponentModel;
using FishingBot.Core.Config;
using System.Windows.Shapes;

namespace FishingBot.App;

public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyPauseId = 9001;
    private const int HotkeyPanicId = 9002;

    private const uint ModNone = 0;
    private const uint VkF9 = 0x78;
    private const uint VkF10 = 0x79;
    private const double ResizeHandleSize = 10;
    private const double MinRegionSizePx = 20;

    private readonly MainViewModel _viewModel;
    private HwndSource? _hwndSource;

    private RoiRegion _activeRegion = RoiRegion.None;
    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private NormalizedRect _originalRegion;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new UiDispatcher(Dispatcher));
        DataContext = _viewModel;
        _viewModel.Calibration.PropertyChanged += OnCalibrationPropertyChanged;
        Loaded += (_, _) => RefreshRoiOverlay();
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

        _viewModel.Calibration.PropertyChanged -= OnCalibrationPropertyChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private enum DragMode
    {
        None,
        Move,
        Resize
    }

    private enum RoiRegion
    {
        None,
        StartPrompt,
        Tension,
        Fight,
        CatchMenu
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

    private void RoiCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RefreshRoiOverlay();
    }

    private void RoiCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RoiCanvas.ActualWidth <= 1 || RoiCanvas.ActualHeight <= 1)
        {
            return;
        }

        var point = e.GetPosition(RoiCanvas);
        if (!TryHitRegion(point, out var region, out var mode))
        {
            return;
        }

        _activeRegion = region;
        _dragMode = mode;
        _dragStart = point;
        _originalRegion = GetRegion(region);
        RoiCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void RoiCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeRegion == RoiRegion.None || _dragMode == DragMode.None)
        {
            return;
        }

        var point = e.GetPosition(RoiCanvas);
        var deltaX = point.X - _dragStart.X;
        var deltaY = point.Y - _dragStart.Y;

        var originalRect = ToCanvasRect(_originalRegion);
        var updatedRect = _dragMode switch
        {
            DragMode.Move => MoveRect(originalRect, deltaX, deltaY),
            DragMode.Resize => ResizeRect(originalRect, deltaX, deltaY),
            _ => originalRect
        };

        var normalized = ToNormalizedRect(updatedRect);
        SetRegion(_activeRegion, normalized);
        RefreshRoiOverlay();
    }

    private void RoiCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_activeRegion == RoiRegion.None)
        {
            return;
        }

        _activeRegion = RoiRegion.None;
        _dragMode = DragMode.None;
        RoiCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnCalibrationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshRoiOverlay();
    }

    private void RefreshRoiOverlay()
    {
        if (RoiCanvas.ActualWidth <= 1 || RoiCanvas.ActualHeight <= 1)
        {
            return;
        }

        UpdateRegionVisuals(GetRegion(RoiRegion.StartPrompt), StartPromptRect, StartPromptHandle, StartPromptLabel);
        UpdateRegionVisuals(GetRegion(RoiRegion.Tension), TensionRect, TensionHandle, TensionLabel);
        UpdateRegionVisuals(GetRegion(RoiRegion.Fight), FightRect, FightHandle, FightLabel);
        UpdateRegionVisuals(GetRegion(RoiRegion.CatchMenu), CatchMenuRect, CatchMenuHandle, CatchMenuLabel);
    }

    private void UpdateRegionVisuals(NormalizedRect region, Rectangle rect, Rectangle handle, TextBlock label)
    {
        var canvasRect = ToCanvasRect(region);

        rect.Width = canvasRect.Width;
        rect.Height = canvasRect.Height;
        Canvas.SetLeft(rect, canvasRect.X);
        Canvas.SetTop(rect, canvasRect.Y);

        Canvas.SetLeft(handle, canvasRect.Right - ResizeHandleSize / 2.0);
        Canvas.SetTop(handle, canvasRect.Bottom - ResizeHandleSize / 2.0);

        Canvas.SetLeft(label, canvasRect.X + 2);
        Canvas.SetTop(label, Math.Max(0, canvasRect.Y - 16));
    }

    private bool TryHitRegion(Point point, out RoiRegion region, out DragMode dragMode)
    {
        foreach (var candidate in new[] { RoiRegion.CatchMenu, RoiRegion.Fight, RoiRegion.Tension, RoiRegion.StartPrompt })
        {
            var rect = ToCanvasRect(GetRegion(candidate));
            if (!rect.Contains(point))
            {
                continue;
            }

            region = candidate;

            var inResizeZone = point.X >= rect.Right - 14 && point.Y >= rect.Bottom - 14;
            dragMode = inResizeZone ? DragMode.Resize : DragMode.Move;
            return true;
        }

        region = RoiRegion.None;
        dragMode = DragMode.None;
        return false;
    }

    private Rect MoveRect(Rect rect, double deltaX, double deltaY)
    {
        var x = Math.Clamp(rect.X + deltaX, 0, Math.Max(0, RoiCanvas.ActualWidth - rect.Width));
        var y = Math.Clamp(rect.Y + deltaY, 0, Math.Max(0, RoiCanvas.ActualHeight - rect.Height));
        return new Rect(x, y, rect.Width, rect.Height);
    }

    private Rect ResizeRect(Rect rect, double deltaX, double deltaY)
    {
        var width = Math.Clamp(rect.Width + deltaX, MinRegionSizePx, RoiCanvas.ActualWidth - rect.X);
        var height = Math.Clamp(rect.Height + deltaY, MinRegionSizePx, RoiCanvas.ActualHeight - rect.Y);
        return new Rect(rect.X, rect.Y, width, height);
    }

    private Rect ToCanvasRect(NormalizedRect region)
    {
        var x = region.X * RoiCanvas.ActualWidth;
        var y = region.Y * RoiCanvas.ActualHeight;
        var width = region.W * RoiCanvas.ActualWidth;
        var height = region.H * RoiCanvas.ActualHeight;
        return new Rect(x, y, width, height);
    }

    private NormalizedRect ToNormalizedRect(Rect rect)
    {
        var x = RoiCanvas.ActualWidth > 0 ? rect.X / RoiCanvas.ActualWidth : 0;
        var y = RoiCanvas.ActualHeight > 0 ? rect.Y / RoiCanvas.ActualHeight : 0;
        var w = RoiCanvas.ActualWidth > 0 ? rect.Width / RoiCanvas.ActualWidth : 0.1;
        var h = RoiCanvas.ActualHeight > 0 ? rect.Height / RoiCanvas.ActualHeight : 0.1;

        return new NormalizedRect(
            Math.Clamp(x, 0, 1),
            Math.Clamp(y, 0, 1),
            Math.Clamp(w, 0.01, 1),
            Math.Clamp(h, 0.01, 1));
    }

    private NormalizedRect GetRegion(RoiRegion region)
    {
        var calibration = _viewModel.Calibration;
        return region switch
        {
            RoiRegion.StartPrompt => new NormalizedRect(calibration.StartPromptX, calibration.StartPromptY, calibration.StartPromptW, calibration.StartPromptH),
            RoiRegion.Tension => new NormalizedRect(calibration.TensionWidgetX, calibration.TensionWidgetY, calibration.TensionWidgetW, calibration.TensionWidgetH),
            RoiRegion.Fight => new NormalizedRect(calibration.FightBarX, calibration.FightBarY, calibration.FightBarW, calibration.FightBarH),
            RoiRegion.CatchMenu => new NormalizedRect(calibration.CatchMenuX, calibration.CatchMenuY, calibration.CatchMenuW, calibration.CatchMenuH),
            _ => new NormalizedRect(0, 0, 0.1, 0.1)
        };
    }

    private void SetRegion(RoiRegion region, NormalizedRect value)
    {
        var calibration = _viewModel.Calibration;

        switch (region)
        {
            case RoiRegion.StartPrompt:
                calibration.StartPromptX = value.X;
                calibration.StartPromptY = value.Y;
                calibration.StartPromptW = value.W;
                calibration.StartPromptH = value.H;
                break;
            case RoiRegion.Tension:
                calibration.TensionWidgetX = value.X;
                calibration.TensionWidgetY = value.Y;
                calibration.TensionWidgetW = value.W;
                calibration.TensionWidgetH = value.H;
                break;
            case RoiRegion.Fight:
                calibration.FightBarX = value.X;
                calibration.FightBarY = value.Y;
                calibration.FightBarW = value.W;
                calibration.FightBarH = value.H;
                break;
            case RoiRegion.CatchMenu:
                calibration.CatchMenuX = value.X;
                calibration.CatchMenuY = value.Y;
                calibration.CatchMenuW = value.W;
                calibration.CatchMenuH = value.H;
                break;
        }
    }
}
