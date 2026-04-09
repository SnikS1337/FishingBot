using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FishingBot.App.Services;
using FishingBot.Core.Capture;
using FishingBot.Core.Config;
using FishingBot.Core.Fsm;
using FishingBot.Core.Input;
using FishingBot.Core.Logging;
using FishingBot.Core.Orchestration;
using FishingBot.Core.Vision;

namespace FishingBot.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly UiDispatcher _uiDispatcher;
    private readonly ConfigService _configService;
    private readonly string _configPath;

    private BotConfig _config;
    private InMemoryLogSink? _logSink;
    private AppOrchestrator? _orchestrator;
    private DateTimeOffset _lastSnapshotUtc;

    private string _statusText = "Ready";
    private string _currentState = FishingState.WaitStartPrompt.ToString();
    private bool _startPromptDetected;
    private double _startPromptConfidence;
    private bool _biteDetected;
    private double _biteConfidence;
    private bool _aimAligned;
    private double _aimConfidence;
    private bool _fightDetected;
    private int _fightMarkerX = -1;
    private bool _catchMenuDetected;
    private double _catchMenuConfidence;
    private double _captureFps;
    private double _visionFps;
    private bool _isRunning;
    private bool _isPaused;
    private string _selectedFishAction;
    private ImageSource? _livePreview;

    private readonly InputEngine _manualInput = new();

    public MainViewModel(UiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher;
        _configService = new ConfigService();
        _configPath = ResolveConfigPath();
        _config = _configService.Load(_configPath);

        Calibration = new CalibrationViewModel();
        Calibration.LoadFrom(_config);

        FishActions = ["TAKE", "RELEASE"];
        _selectedFishAction = _config.Fishing.Action.ToUpperInvariant();

        StartDetectOnlyCommand = new RelayCommand(() => Start(activeMode: false));
        StartActiveCommand = new RelayCommand(() => Start(activeMode: true));
        PauseResumeCommand = new RelayCommand(PauseResume);
        StopCommand = new RelayCommand(Stop);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        ReloadConfigCommand = new RelayCommand(ReloadConfig);
        TestACommand = new RelayCommand(TestAKey);
        TestDCommand = new RelayCommand(TestDKey);
        TestSpaceCommand = new RelayCommand(TestSpaceKey);

        AddLog("INFO", "CONFIG", $"Config loaded from {_configPath}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CalibrationViewModel Calibration { get; }

    public IReadOnlyList<string> FishActions { get; }

    public ObservableCollection<string> Logs { get; } = [];

    public ICommand StartDetectOnlyCommand { get; }

    public ICommand StartActiveCommand { get; }

    public ICommand PauseResumeCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand SaveConfigCommand { get; }

    public ICommand ReloadConfigCommand { get; }

    public ICommand TestACommand { get; }

    public ICommand TestDCommand { get; }

    public ICommand TestSpaceCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public bool StartPromptDetected
    {
        get => _startPromptDetected;
        private set => SetProperty(ref _startPromptDetected, value);
    }

    public double StartPromptConfidence
    {
        get => _startPromptConfidence;
        private set => SetProperty(ref _startPromptConfidence, value);
    }

    public bool BiteDetected
    {
        get => _biteDetected;
        private set => SetProperty(ref _biteDetected, value);
    }

    public bool AimAligned
    {
        get => _aimAligned;
        private set => SetProperty(ref _aimAligned, value);
    }

    public double AimConfidence
    {
        get => _aimConfidence;
        private set => SetProperty(ref _aimConfidence, value);
    }

    public double BiteConfidence
    {
        get => _biteConfidence;
        private set => SetProperty(ref _biteConfidence, value);
    }

    public bool FightDetected
    {
        get => _fightDetected;
        private set => SetProperty(ref _fightDetected, value);
    }

    public int FightMarkerX
    {
        get => _fightMarkerX;
        private set => SetProperty(ref _fightMarkerX, value);
    }

    public bool CatchMenuDetected
    {
        get => _catchMenuDetected;
        private set => SetProperty(ref _catchMenuDetected, value);
    }

    public double CatchMenuConfidence
    {
        get => _catchMenuConfidence;
        private set => SetProperty(ref _catchMenuConfidence, value);
    }

    public double CaptureFps
    {
        get => _captureFps;
        private set => SetProperty(ref _captureFps, value);
    }

    public double VisionFps
    {
        get => _visionFps;
        private set => SetProperty(ref _visionFps, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (SetProperty(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(PauseResumeText));
            }
        }
    }

    public string PauseResumeText => IsPaused ? "Resume" : "Pause";

    public string SelectedFishAction
    {
        get => _selectedFishAction;
        set => SetProperty(ref _selectedFishAction, value);
    }

    public ImageSource? LivePreview
    {
        get => _livePreview;
        private set => SetProperty(ref _livePreview, value);
    }

    public void Dispose()
    {
        if (_orchestrator is not null)
        {
            _orchestrator.StateChanged -= OnStateChanged;
            _orchestrator.SnapshotUpdated -= OnSnapshotUpdated;
            _orchestrator.PreviewFrameUpdated -= OnPreviewFrameUpdated;
            _orchestrator.Dispose();
            _orchestrator = null;
        }

        if (_logSink is not null)
        {
            _logSink.EntryWritten -= OnLogWritten;
            _logSink = null;
        }
    }

    public void TriggerPauseResumeFromHotkey()
    {
        PauseResume();
    }

    public void TriggerPanicStopFromHotkey()
    {
        if (_orchestrator is null)
        {
            return;
        }

        _orchestrator.PanicStop();
        IsRunning = false;
        IsPaused = false;
        StatusText = "Panic stop";
        CurrentState = FishingState.WaitStartPrompt.ToString();
    }

    private void Start(bool activeMode)
    {
        if (!EnsureOrchestrator())
        {
            return;
        }

        ApplySettingsToConfig();
        _configService.Save(_configPath, _config);

        if (activeMode)
        {
            _orchestrator!.StartActive();
            StatusText = "Active mode running";
            AddLog("INFO", "MODE", "Started Active mode");
        }
        else
        {
            _orchestrator!.StartDetectOnly();
            StatusText = "DetectOnly mode running";
            AddLog("INFO", "MODE", "Started DetectOnly mode");
        }

        IsRunning = true;
        IsPaused = false;
    }

    private void PauseResume()
    {
        if (_orchestrator is null)
        {
            return;
        }

        if (_orchestrator.IsPaused)
        {
            _orchestrator.Resume();
            IsPaused = false;
            StatusText = "Resumed";
        }
        else
        {
            _orchestrator.Pause();
            IsPaused = true;
            StatusText = "Paused";
        }
    }

    private void Stop()
    {
        if (_orchestrator is null)
        {
            return;
        }

        _orchestrator.Stop();
        IsRunning = false;
        IsPaused = false;
        StatusText = "Stopped";
        CurrentState = FishingState.WaitStartPrompt.ToString();
    }

    private void SaveConfig()
    {
        ApplySettingsToConfig();
        _configService.Save(_configPath, _config);
        StatusText = "Config saved";
        AddLog("INFO", "CONFIG", "Configuration saved");
    }

    private void ReloadConfig()
    {
        _config = _configService.Load(_configPath);
        Calibration.LoadFrom(_config);
        SelectedFishAction = _config.Fishing.Action.ToUpperInvariant();
        StatusText = "Config reloaded";
        AddLog("INFO", "CONFIG", "Configuration reloaded");
    }

    private void TestAKey()
    {
        _manualInput.HoldA();
        Thread.Sleep(120);
        _manualInput.ReleaseAD();
        AddLog("INFO", "TEST_KEY", "Pressed test key A");
    }

    private void TestDKey()
    {
        _manualInput.HoldD();
        Thread.Sleep(120);
        _manualInput.ReleaseAD();
        AddLog("INFO", "TEST_KEY", "Pressed test key D");
    }

    private void TestSpaceKey()
    {
        _manualInput.PressSpace();
        AddLog("INFO", "TEST_KEY", "Pressed test key Space");
    }

    private bool EnsureOrchestrator()
    {
        if (_orchestrator is not null)
        {
            return true;
        }

        try
        {
            _logSink = new InMemoryLogSink(500);
            _logSink.EntryWritten += OnLogWritten;

            var capture = new DxgiCaptureEngine();
            var vision = new VisionPipeline(
                new StartPromptDetector(),
                new AimDetector(),
                new TensionDetector(),
                new FightDetector(),
                new CatchMenuDetector(
                    GetTemplatePath("btn_take.png"),
                    GetTemplatePath("btn_release.png")));
            var input = new InputEngine();

            _orchestrator = new AppOrchestrator(
                capture,
                vision,
                input,
                _logSink,
                _config);

            _orchestrator.StateChanged += OnStateChanged;
            _orchestrator.SnapshotUpdated += OnSnapshotUpdated;
            _orchestrator.PreviewFrameUpdated += OnPreviewFrameUpdated;

            AddLog("INFO", "RUNTIME", "Runtime initialized");
            return true;
        }
        catch (Exception ex)
        {
            StatusText = "Initialization failed";
            AddLog("ERROR", "INIT_FAIL", ex.Message);
            return false;
        }
    }

    private void ApplySettingsToConfig()
    {
        _config.Fishing.Action = SelectedFishAction.ToUpperInvariant();
        Calibration.ApplyTo(_config);
    }

    private string GetTemplatePath(string fileName)
    {
        var projectRoot = Path.GetDirectoryName(_configPath) ?? Environment.CurrentDirectory;
        return Path.Combine(projectRoot, "templates", "fishing", fileName);
    }

    private void OnStateChanged(FishingState state)
    {
        _uiDispatcher.Post(() =>
        {
            CurrentState = state.ToString();
        });
    }

    private void OnSnapshotUpdated(VisionSnapshot snapshot)
    {
        _uiDispatcher.Post(() =>
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastSnapshotUtc != default)
            {
                var deltaSeconds = (now - _lastSnapshotUtc).TotalSeconds;
                if (deltaSeconds > 0)
                {
                    var fps = Math.Round(1.0 / deltaSeconds, 1);
                    CaptureFps = fps;
                    VisionFps = fps;
                }
            }

            _lastSnapshotUtc = now;

            StartPromptDetected = snapshot.StartPromptDetected;
            StartPromptConfidence = Math.Round(snapshot.StartPromptConfidence, 3);
            BiteDetected = snapshot.BiteDetected;
            BiteConfidence = Math.Round(snapshot.BiteConfidence, 3);
            AimAligned = snapshot.AimAligned;
            AimConfidence = Math.Round(snapshot.AimConfidence, 3);
            FightDetected = snapshot.FightDetected;
            FightMarkerX = snapshot.FightMarkerX;
            CatchMenuDetected = snapshot.CatchMenuDetected;
            CatchMenuConfidence = Math.Round(snapshot.CatchMenuConfidence, 3);
        });
    }

    private void OnLogWritten(LogEntry entry)
    {
        _uiDispatcher.Post(() => AddLog(entry.Level, entry.Event, entry.Message, entry.TimestampUtc));
    }

    private void OnPreviewFrameUpdated(byte[] frameBytes)
    {
        var image = BuildImage(frameBytes);
        _uiDispatcher.Post(() => LivePreview = image);
    }

    private void AddLog(string level, string evt, string message, DateTimeOffset? timestamp = null)
    {
        var at = timestamp ?? DateTimeOffset.UtcNow;
        var line = $"[{at:HH:mm:ss}] {level} {evt}: {message}";
        Logs.Add(line);

        while (Logs.Count > 500)
        {
            Logs.RemoveAt(0);
        }
    }

    private static string ResolveConfigPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "config.json"),
            Path.Combine(AppContext.BaseDirectory, "config.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static BitmapSource BuildImage(byte[] frameBytes)
    {
        using var stream = new MemoryStream(frameBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
