using FishingBot.Core.Contracts;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.DXGI.Resource;

namespace FishingBot.Core.Capture;

public sealed class DxgiCaptureEngine : ICaptureEngine
{
    private const int DxgiErrorAccessLost = unchecked((int)0x887A0026);
    private const int DxgiErrorWaitTimeout = unchecked((int)0x887A0027);
    private const int DxgiErrorSessionDisconnected = unchecked((int)0x887A0028);

    private readonly object _sync = new();
    private readonly int _acquireTimeoutMs;

    private Factory1? _factory;
    private Adapter1? _adapter;
    private Output? _output;
    private Output1? _output1;
    private Device? _device;
    private OutputDuplication? _duplication;
    private Texture2D? _stagingTexture;

    private int _frameWidth;
    private int _frameHeight;
    private bool _disposed;

    public DxgiCaptureEngine(int acquireTimeoutMs = 8)
    {
        _acquireTimeoutMs = acquireTimeoutMs;
        Initialize();
    }

    public bool TryGetLatestFrame(out CapturedFrame frame)
    {
        lock (_sync)
        {
            frame = null!;
            if (_disposed || _device is null || _duplication is null || _stagingTexture is null)
            {
                return false;
            }

            var acquiredFrame = false;
            Resource? desktopResource = null;

            try
            {
                var result = _duplication.TryAcquireNextFrame(
                    _acquireTimeoutMs,
                    out _,
                    out desktopResource);

                if (result.Failure)
                {
                    if (result.Code == DxgiErrorWaitTimeout)
                    {
                        return false;
                    }

                    if (result.Code is DxgiErrorAccessLost or DxgiErrorSessionDisconnected)
                    {
                        Reinitialize();
                        return false;
                    }

                    result.CheckError();
                    return false;
                }

                acquiredFrame = true;

                if (desktopResource is null)
                {
                    return false;
                }

                using var screenTexture = desktopResource.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopyResource(screenTexture, _stagingTexture);

                var dataBox = _device.ImmediateContext.MapSubresource(
                    _stagingTexture,
                    0,
                    MapMode.Read,
                    SharpDX.Direct3D11.MapFlags.None);

                try
                {
                    using var bgraView = Mat.FromPixelData(
                        _frameHeight,
                        _frameWidth,
                        MatType.CV_8UC4,
                        dataBox.DataPointer,
                        dataBox.RowPitch);

                    using var bgraClone = bgraView.Clone();
                    var bgr = new Mat();
                    Cv2.CvtColor(bgraClone, bgr, ColorConversionCodes.BGRA2BGR);

                    frame = new CapturedFrame(bgr, DateTimeOffset.UtcNow);
                    return true;
                }
                finally
                {
                    _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
                }
            }
            catch (SharpDXException ex) when (ex.HResult == DxgiErrorAccessLost || ex.HResult == DxgiErrorSessionDisconnected)
            {
                Reinitialize();
                return false;
            }
            finally
            {
                desktopResource?.Dispose();

                if (acquiredFrame && _duplication is not null)
                {
                    try
                    {
                        _duplication.ReleaseFrame();
                    }
                    catch
                    {
                        // Игнорируем — следующий цикл обработает переинициализацию при необходимости.
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DisposeResources();
        }
    }

    private void Initialize()
    {
        DisposeResources();

        _factory = new Factory1();
        _adapter = _factory.GetAdapter1(0);
        _output = _adapter.GetOutput(0);
        _output1 = _output.QueryInterface<Output1>();

        _device = new Device(_adapter);
        _duplication = _output1.DuplicateOutput(_device);

        var bounds = _output.Description.DesktopBounds;
        _frameWidth = bounds.Right - bounds.Left;
        _frameHeight = bounds.Bottom - bounds.Top;

        _stagingTexture = new Texture2D(_device, new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = _frameWidth,
            Height = _frameHeight,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        });
    }

    private void Reinitialize()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            Initialize();
        }
        catch
        {
            // Оставляем engine в fail-soft состоянии. Следующая попытка снова вызовет TryGetLatestFrame.
        }
    }

    private void DisposeResources()
    {
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        _duplication?.Dispose();
        _duplication = null;

        _device?.Dispose();
        _device = null;

        _output1?.Dispose();
        _output1 = null;

        _output?.Dispose();
        _output = null;

        _adapter?.Dispose();
        _adapter = null;

        _factory?.Dispose();
        _factory = null;
    }
}
