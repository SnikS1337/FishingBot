using System.Drawing;

namespace FishingBot.Core.Input;

public sealed class InputEngine
{
    private readonly BezierMouseMover _mouseMover;
    private readonly Random _random;

    private bool _isAHeld;
    private bool _isDHeld;

    public InputEngine(BezierMouseMover? mouseMover = null, Random? random = null)
    {
        _mouseMover = mouseMover ?? new BezierMouseMover();
        _random = random ?? new Random();
    }

    public void PressE()
    {
        SendInputNative.PressKey(ScanCodes.E);
    }

    public void PressSpace()
    {
        SendInputNative.PressKey(ScanCodes.Space);
    }

    public void HoldA()
    {
        if (_isAHeld)
        {
            return;
        }

        if (_isDHeld)
        {
            SendInputNative.KeyUp(ScanCodes.D);
            _isDHeld = false;
        }

        SendInputNative.KeyDown(ScanCodes.A);
        _isAHeld = true;
    }

    public void HoldD()
    {
        if (_isDHeld)
        {
            return;
        }

        if (_isAHeld)
        {
            SendInputNative.KeyUp(ScanCodes.A);
            _isAHeld = false;
        }

        SendInputNative.KeyDown(ScanCodes.D);
        _isDHeld = true;
    }

    public void ReleaseAD()
    {
        if (_isAHeld)
        {
            SendInputNative.KeyUp(ScanCodes.A);
            _isAHeld = false;
        }

        if (_isDHeld)
        {
            SendInputNative.KeyUp(ScanCodes.D);
            _isDHeld = false;
        }
    }

    public void ReleaseAll()
    {
        ReleaseAD();
        SendInputNative.KeyUp(ScanCodes.E);
        SendInputNative.KeyUp(ScanCodes.Space);
    }

    public void ClickAt(Point from, Point to)
    {
        _mouseMover.MoveMouse(from, to);
        Thread.Sleep(_random.Next(80, 200));
        SendInputNative.LeftClick();
    }
}
