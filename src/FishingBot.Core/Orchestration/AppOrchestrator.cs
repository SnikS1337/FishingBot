using System.Drawing;
using FishingBot.Core.Capture;
using FishingBot.Core.Config;
using FishingBot.Core.Contracts;
using FishingBot.Core.Fsm;
using FishingBot.Core.Logging;
using FishingBot.Core.Vision;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;

namespace FishingBot.Core.Orchestration;

public sealed class AppOrchestrator : IDisposable
{
    private readonly object _lifecycleSync = new();
    private readonly ICaptureEngine _captureEngine;
    private readonly IVisionPipeline _visionPipeline;
    private readonly IInputEngine _inputEngine;
    private readonly ILogSink _logSink;
    private readonly BotConfig _config;
    private readonly StateTimeouts _timeouts;
    private readonly Random _random;

    private FishingStateMachine _fsm;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private bool _isPaused;
    private bool _isActiveMode;
    private bool _entryActionPending;

    private DateTimeOffset _stateEnteredUtc;
    private int _previousFightMarkerX;
    private int _lastFightDirection;
    private int _startPromptSeenFrames;
    private DateTimeOffset _castAwaitStartedUtc;

    public AppOrchestrator(
        ICaptureEngine captureEngine,
        IVisionPipeline visionPipeline,
        IInputEngine inputEngine,
        ILogSink logSink,
        BotConfig config,
        StateTimeouts? timeouts = null,
        Random? random = null)
    {
        _captureEngine = captureEngine;
        _visionPipeline = visionPipeline;
        _inputEngine = inputEngine;
        _logSink = logSink;
        _config = config;
        _timeouts = timeouts ?? new StateTimeouts();
        _random = random ?? new Random();

        _fsm = new FishingStateMachine(FishingState.WaitStartPrompt);
        _stateEnteredUtc = DateTimeOffset.UtcNow;
        _entryActionPending = true;
        _previousFightMarkerX = -1;
        _lastFightDirection = 0;
        _startPromptSeenFrames = 0;
        _castAwaitStartedUtc = default;
    }

    public event Action<FishingState>? StateChanged;
    public event Action<VisionSnapshot>? SnapshotUpdated;
    public event Action<byte[]>? PreviewFrameUpdated;

    public FishingState CurrentState => _fsm.Current;
    public bool IsRunning => _loopTask is { IsCompleted: false };
    public bool IsPaused => _isPaused;

    public void StartDetectOnly() => StartInternal(activeMode: false);
    public void StartActive() => StartInternal(activeMode: true);

    public void Pause()
    {
        _isPaused = true;
        _inputEngine.ReleaseAll();
        Log("INFO", "PAUSED", "Execution paused.");
    }

    public void Resume()
    {
        _isPaused = false;
        Log("INFO", "RESUMED", "Execution resumed.");
    }

    public void PanicStop()
    {
        Stop();
        Log("WARN", "PANIC_TRIGGERED", "Panic stop executed.");
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task? loopTask;

        lock (_lifecycleSync)
        {
            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;
            _isPaused = false;
            _isActiveMode = false;
        }

        cts?.Cancel();

        try
        {
            loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        finally
        {
            cts?.Dispose();
        }

        _inputEngine.ReleaseAll();
        ApplyEvent(FishingEvent.Reset, "manual stop");
        Log("INFO", "STOPPED", "Execution stopped.");
    }

    public void Dispose() => Stop();

    private void StartInternal(bool activeMode)
    {
        Stop();

        lock (_lifecycleSync)
        {
            _isActiveMode = activeMode;
            _isPaused = false;
            _fsm = new FishingStateMachine(FishingState.WaitStartPrompt);
            _stateEnteredUtc = DateTimeOffset.UtcNow;
            _entryActionPending = true;
            _previousFightMarkerX = -1;
            _lastFightDirection = 0;
            _startPromptSeenFrames = 0;
            _castAwaitStartedUtc = default;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        }

        Log("INFO", "STARTED", activeMode ? "Active mode started." : "DetectOnly mode started.");
        StateChanged?.Invoke(_fsm.Current);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_isPaused)
            {
                await DelaySafe(50, token);
                continue;
            }

            try
            {
                Tick();
            }
            catch (Exception ex)
            {
                Log("ERROR", "LOOP_EXCEPTION", ex.Message);
            }

            await DelaySafe(5, token);
        }
    }

    private void Tick()
    {
        if (!_captureEngine.TryGetLatestFrame(out var frame))
            return;

        using (frame)
        {
            // Два региона для StartPrompt — правый нижний и верхний левый
            using var startPromptRoi    = Crop(frame.BgrFrame, _config.Regions.StartPrompt);
            using var startPromptAltRoi = Crop(frame.BgrFrame, _config.Regions.StartPromptAlt);
            using var aimRoi            = Crop(frame.BgrFrame, _config.Regions.AimBar);
            using var tensionRoi        = Crop(frame.BgrFrame, _config.Regions.TensionWidget);
            using var fightRoi          = Crop(frame.BgrFrame, _config.Regions.FightBar);
            using var catchMenuRoi      = Crop(frame.BgrFrame, _config.Regions.CatchMenu);

            var snapshot = _visionPipeline.Analyze(
                startPromptRoi, startPromptAltRoi,
                aimRoi, tensionRoi, fightRoi, catchMenuRoi);

            SnapshotUpdated?.Invoke(snapshot);
            PublishPreviewFrame(frame.BgrFrame, snapshot);

            CheckStateTimeout();
            ProcessVisionEvents(snapshot);
            ExecuteStateActions(snapshot);
        }
    }

    private void CheckStateTimeout()
    {
        var timeoutMs = GetTimeoutForState(_fsm.Current);
        if (timeoutMs <= 0) return;

        var elapsed = DateTimeOffset.UtcNow - _stateEnteredUtc;
        if (elapsed.TotalMilliseconds < timeoutMs) return;

        _inputEngine.ReleaseAll();
        ApplyEvent(FishingEvent.Timeout, $"state {_fsm.Current} timeout ({timeoutMs}ms)");
    }

    private void ProcessVisionEvents(VisionSnapshot snapshot)
    {
        if (!snapshot.StartPromptDetected)
        {
            _startPromptSeenFrames = 0;
        }

        switch (_fsm.Current)
        {
            case FishingState.WaitStartPrompt when IsStartPromptConfirmed(snapshot):
                ApplyEvent(FishingEvent.StartPromptDetected, "start prompt detected");
                break;

            case FishingState.WaitSecondStartPrompt when IsStartPromptConfirmed(snapshot):
                ApplyEvent(FishingEvent.StartPromptDetected, "second start prompt detected");
                break;

            case FishingState.WaitBite when snapshot.BiteDetected:
                ApplyEvent(FishingEvent.BiteDetected, "tension red detected");
                break;

            case FishingState.Fight when snapshot.CatchMenuDetected:
                ApplyEvent(FishingEvent.CatchMenuDetected, "catch menu detected");
                break;
        }
    }

    private void ExecuteStateActions(VisionSnapshot snapshot)
    {
        switch (_fsm.Current)
        {
            case FishingState.EnterFishingMode:
                ExecuteEntryAction(() =>
                {
                    if (CanAct())
                    {
                        RandomDelay(_config.Timing.ActionDelayMin, _config.Timing.ActionDelayMax);
                        _inputEngine.PressE();
                        Log("INFO", "PRESS_E", "Pressed first E to enter fishing mode.");
                        Thread.Sleep(400);
                    }

                    ApplyEvent(FishingEvent.StartFishingDone, "entered fishing mode and waiting second prompt");
                });
                break;

            case FishingState.StartFishing:
                ExecuteEntryAction(() =>
                {
                    if (CanAct())
                    {
                        RandomDelay(_config.Timing.ActionDelayMin, _config.Timing.ActionDelayMax);
                        _inputEngine.PressE();
                        Log("INFO", "PRESS_E", "Pressed second E to start fishing.");
                        Thread.Sleep(400);
                    }

                    _castAwaitStartedUtc = DateTimeOffset.UtcNow;
                });

                if (snapshot.AimAligned)
                {
                    if (CanAct())
                    {
                        RandomDelay(_config.Timing.ActionDelayMin, _config.Timing.ActionDelayMax);
                        _inputEngine.PressSpace();
                        Log("INFO", "AIM_SPACE", "Aim aligned, pressed Space to cast.");
                    }

                    ApplyEvent(FishingEvent.StartFishingDone, "aim aligned and cast confirmed");
                }
                else if (IsCastFallbackDue())
                {
                    if (CanAct())
                    {
                        RandomDelay(_config.Timing.ActionDelayMin, _config.Timing.ActionDelayMax);
                        _inputEngine.PressSpace();
                        Log("WARN", "CAST_FALLBACK_SPACE", "Aim timeout reached, forced Space cast.");
                    }

                    ApplyEvent(FishingEvent.StartFishingDone, "cast fallback timeout reached");
                }
                break;

            case FishingState.Hook:
                ExecuteEntryAction(() =>
                {
                    if (CanAct())
                    {
                        RandomDelay(_config.Timing.ActionDelayMin, _config.Timing.ActionDelayMax);
                        _inputEngine.PressSpace();
                        Log("INFO", "PRESS_SPACE", "Pressed Space to hook.");
                    }

                    ApplyEvent(FishingEvent.HookDone, "hook action done");
                });
                break;

            case FishingState.Fight:
                _entryActionPending = false;
                HandleFight(snapshot);
                break;

            case FishingState.CatchMenu:
                ExecuteEntryAction(() =>
                {
                    if (CanAct())
                    {
                        RandomDelay(300, 700);
                        _inputEngine.ClickAt(ResolveCatchButtonPoint());
                        Log("INFO", "CATCH_ACTION", $"Applied action: {_config.Fishing.Action}.");
                    }

                    ApplyEvent(FishingEvent.ActionApplied, "catch menu action done");
                });
                break;

            case FishingState.Recast:
                ExecuteEntryAction(() =>
                {
                    if (CanAct())
                    {
                        _inputEngine.ReleaseAD();
                        var min = _config.Fishing.RecastDelayMs.ElementAtOrDefault(0);
                        var max = _config.Fishing.RecastDelayMs.ElementAtOrDefault(1);
                        RandomDelay(min <= 0 ? 1000 : min, max <= min ? min + 1 : max);
                        _inputEngine.PressSpace();
                        Log("INFO", "RECAST", "Performed recast.");
                    }

                    ApplyEvent(FishingEvent.RecastDone, "recast done");
                });
                break;
        }
    }

    private void HandleFight(VisionSnapshot snapshot)
    {
        if (!CanAct() || !snapshot.FightDetected || snapshot.FightMarkerX < 0)
            return;

        if (_previousFightMarkerX < 0)
        {
            var fightRect = _config.Regions.FightBar.ToPixelRect(_config.Resolution.W, _config.Resolution.H);
            var centerX = Math.Max(1, fightRect.Width) / 2;

            if (snapshot.FightMarkerX >= centerX)
            {
                _lastFightDirection = 1;
                _inputEngine.HoldA();
            }
            else
            {
                _lastFightDirection = -1;
                _inputEngine.HoldD();
            }

            _previousFightMarkerX = snapshot.FightMarkerX;
            return;
        }

        var delta = snapshot.FightMarkerX - _previousFightMarkerX;
        if (delta > 0)
        {
            _lastFightDirection = 1;
            _inputEngine.HoldA();
        }
        else if (delta < 0)
        {
            _lastFightDirection = -1;
            _inputEngine.HoldD();
        }
        else if (_lastFightDirection > 0)
        {
            _inputEngine.HoldA();
        }
        else if (_lastFightDirection < 0)
        {
            _inputEngine.HoldD();
        }

        _previousFightMarkerX = snapshot.FightMarkerX;
    }

    private void ExecuteEntryAction(Action action)
    {
        if (!_entryActionPending) return;
        _entryActionPending = false;
        action();
    }

    private void ApplyEvent(FishingEvent evt, string reason)
    {
        var before = _fsm.Current;
        _fsm.Handle(evt);
        var after = _fsm.Current;

        if (before == after) return;

        _stateEnteredUtc = DateTimeOffset.UtcNow;
        _entryActionPending = true;
        _previousFightMarkerX = -1;
        _lastFightDirection = 0;
        _startPromptSeenFrames = 0;
        _castAwaitStartedUtc = default;

        Log("INFO", "STATE_CHANGE", $"{before} -> {after} ({reason})");
        StateChanged?.Invoke(after);
    }

    private bool IsStartPromptConfirmed(VisionSnapshot snapshot)
    {
        if (!snapshot.StartPromptDetected)
        {
            _startPromptSeenFrames = 0;
            return false;
        }

        _startPromptSeenFrames++;
        var requiredFrames = Math.Max(1, _config.Detection.StartPromptConfirmFrames);
        return _startPromptSeenFrames >= requiredFrames;
    }

    private bool IsCastFallbackDue()
    {
        if (_castAwaitStartedUtc == default)
        {
            return false;
        }

        return CastTimingPolicy.ShouldForceCast(_castAwaitStartedUtc, DateTimeOffset.UtcNow, timeoutMs: 1500);
    }

    private int GetTimeoutForState(FishingState state)
    {
        return state switch
        {
            FishingState.WaitStartPrompt       => _timeouts.WaitStartPromptMs,
            FishingState.EnterFishingMode      => _timeouts.StartFishingMs,
            FishingState.WaitSecondStartPrompt => _timeouts.WaitStartPromptMs,
            FishingState.StartFishing          => _timeouts.StartFishingMs,
            FishingState.WaitBite              => _timeouts.WaitBiteMs,
            FishingState.Fight                 => _timeouts.FightMs,
            FishingState.CatchMenu             => _timeouts.CatchMenuMs,
            _ => 0
        };
    }

    private Mat Crop(Mat frame, NormalizedRect region)
    {
        var rect   = region.ToPixelRect(frame.Width, frame.Height);
        var x      = Math.Clamp(rect.X, 0, Math.Max(0, frame.Width - 1));
        var y      = Math.Clamp(rect.Y, 0, Math.Max(0, frame.Height - 1));
        var width  = Math.Clamp(rect.Width,  1, frame.Width  - x);
        var height = Math.Clamp(rect.Height, 1, frame.Height - y);
        return new Mat(frame, new OpenCvSharp.Rect(x, y, width, height)).Clone();
    }

    private DrawingPoint ResolveCatchButtonPoint()
    {
        var action = _config.Fishing.Action?.ToUpperInvariant() ?? "RELEASE";
        var menu   = _config.Regions.CatchMenu.ToPixelRect(_config.Resolution.W, _config.Resolution.H);

        var xFactor = action == "TAKE" ? 0.32 : 0.68;
        var yFactor = 0.86;

        var x = menu.X + (int)(menu.Width  * xFactor);
        var y = menu.Y + (int)(menu.Height * yFactor);
        return new DrawingPoint(x, y);
    }

    private bool CanAct() => _isActiveMode && !_isPaused;

    private void RandomDelay(int minMs, int maxMs)
    {
        var min = Math.Max(0, minMs);
        var max = Math.Max(min + 1, maxMs);
        Thread.Sleep(_random.Next(min, max));
    }

    private void Log(string level, string evt, string message)
        => _logSink.Write(new LogEntry(DateTimeOffset.UtcNow, level, evt, message));

    private void PublishPreviewFrame(Mat sourceFrame, VisionSnapshot snapshot)
    {
        if (PreviewFrameUpdated is null) return;

        using var preview = sourceFrame.Clone();
        DrawDebugOverlays(preview, snapshot);

        var encoded = preview.ImEncode(".jpg", [new ImageEncodingParam(ImwriteFlags.JpegQuality, 70)]);
        PreviewFrameUpdated(encoded);
    }

    private void DrawDebugOverlays(Mat frame, VisionSnapshot snapshot)
    {
        DrawRegion(frame, _config.Regions.StartPrompt,    Scalar.Aqua,       "StartPrompt");
        DrawRegion(frame, _config.Regions.StartPromptAlt, Scalar.Cyan,       "StartPromptAlt");
        DrawRegion(frame, _config.Regions.AimBar,         Scalar.GreenYellow,"AimBar");
        DrawRegion(frame, _config.Regions.TensionWidget,  Scalar.Orange,     "Tension");
        DrawRegion(frame, _config.Regions.FightBar,       Scalar.Yellow,     "FightBar");
        DrawRegion(frame, _config.Regions.CatchMenu,      Scalar.LightGreen, "CatchMenu");

        var markerText = snapshot.FightMarkerX >= 0 ? snapshot.FightMarkerX.ToString() : "-";
        var status = $"State:{_fsm.Current} Start:{snapshot.StartPromptDetected} Aim:{snapshot.AimAligned} Bite:{snapshot.BiteDetected} Fight:{snapshot.FightDetected} Marker:{markerText} Menu:{snapshot.CatchMenuDetected}";
        Cv2.PutText(frame, status, new OpenCvSharp.Point(20, 30),
            HersheyFonts.HersheySimplex, 0.65, Scalar.Lime, 2, LineTypes.AntiAlias);
    }

    private void DrawRegion(Mat frame, NormalizedRect region, Scalar color, string label)
    {
        var rect   = region.ToPixelRect(frame.Width, frame.Height);
        var x      = Math.Clamp(rect.X, 0, Math.Max(0, frame.Width - 1));
        var y      = Math.Clamp(rect.Y, 0, Math.Max(0, frame.Height - 1));
        var width  = Math.Clamp(rect.Width,  1, frame.Width  - x);
        var height = Math.Clamp(rect.Height, 1, frame.Height - y);

        Cv2.Rectangle(frame, new OpenCvSharp.Rect(x, y, width, height), color, 2, LineTypes.AntiAlias);
        Cv2.PutText(frame, label, new OpenCvSharp.Point(x, Math.Max(15, y - 6)),
            HersheyFonts.HersheySimplex, 0.55, color, 2, LineTypes.AntiAlias);
    }

    private static async Task DelaySafe(int delayMs, CancellationToken token)
    {
        try { await Task.Delay(delayMs, token); }
        catch (OperationCanceledException) { }
    }
}
