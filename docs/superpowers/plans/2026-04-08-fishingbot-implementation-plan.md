# FishingBot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Построить MVP FishingBot на .NET 8 (WPF) с полным циклом: детект «Нажмите E» → старт рыбалки → подсечка → fight (A/D) → меню поимки → действие TAKE/RELEASE → перезаброс, с режимами DetectOnly/Active.

**Architecture:** Одно приложение с четкими модулями: Capture (DXGI), Vision (детекторы), FSM (состояния и таймауты), Input (SendInput/Bezier), GUI (WPF/MVVM), Config/Logging. ROI хранятся в нормализованном формате и конвертируются в пиксели во время выполнения. Переходы FSM выполняются только при confidence + N подтверждений.

**Tech Stack:** C# .NET 8, WPF, SharpDX (DXGI/Desktop Duplication), OpenCvSharp4, Newtonsoft.Json, xUnit.

---

## File Structure (target)

**Create (solution + projects):**
- `FishingBot.sln`
- `src/FishingBot.App/FishingBot.App.csproj`
- `src/FishingBot.Core/FishingBot.Core.csproj`
- `tests/FishingBot.Tests/FishingBot.Tests.csproj`

**Create (App/WPF):**
- `src/FishingBot.App/App.xaml`
- `src/FishingBot.App/App.xaml.cs`
- `src/FishingBot.App/MainWindow.xaml`
- `src/FishingBot.App/MainWindow.xaml.cs`
- `src/FishingBot.App/ViewModels/MainViewModel.cs`
- `src/FishingBot.App/ViewModels/CalibrationViewModel.cs`
- `src/FishingBot.App/Services/UiDispatcher.cs`

**Create (Core/Config/Contracts):**
- `src/FishingBot.Core/Config/BotConfig.cs`
- `src/FishingBot.Core/Config/NormalizedRect.cs`
- `src/FishingBot.Core/Config/ConfigService.cs`
- `src/FishingBot.Core/Contracts/ICaptureEngine.cs`
- `src/FishingBot.Core/Contracts/IVisionPipeline.cs`
- `src/FishingBot.Core/Contracts/IInputEngine.cs`
- `src/FishingBot.Core/Contracts/ILogSink.cs`
- `src/FishingBot.Core/Contracts/ITimeProvider.cs`

**Create (Core/Capture):**
- `src/FishingBot.Core/Capture/DxgiCaptureEngine.cs`
- `src/FishingBot.Core/Capture/CapturedFrame.cs`

**Create (Core/Vision):**
- `src/FishingBot.Core/Vision/VisionPipeline.cs`
- `src/FishingBot.Core/Vision/VisionSnapshot.cs`
- `src/FishingBot.Core/Vision/StartPromptDetector.cs`
- `src/FishingBot.Core/Vision/TensionDetector.cs`
- `src/FishingBot.Core/Vision/FightDetector.cs`
- `src/FishingBot.Core/Vision/CatchMenuDetector.cs`

**Create (Core/FSM):**
- `src/FishingBot.Core/Fsm/FishingState.cs`
- `src/FishingBot.Core/Fsm/FishingEvent.cs`
- `src/FishingBot.Core/Fsm/FishingStateMachine.cs`
- `src/FishingBot.Core/Fsm/StateTimeouts.cs`

**Create (Core/Input):**
- `src/FishingBot.Core/Input/SendInputNative.cs`
- `src/FishingBot.Core/Input/InputEngine.cs`
- `src/FishingBot.Core/Input/BezierMouseMover.cs`
- `src/FishingBot.Core/Input/ScanCodes.cs`

**Create (Core/Orchestration/Logging):**
- `src/FishingBot.Core/Orchestration/AppOrchestrator.cs`
- `src/FishingBot.Core/Logging/InMemoryLogSink.cs`
- `src/FishingBot.Core/Logging/LogEntry.cs`

**Create (runtime/config/assets/docs):**
- `config.json`
- `templates/fishing/btn_take.png`
- `templates/fishing/btn_release.png`
- `README.md`

**Create (tests):**
- `tests/FishingBot.Tests/Fsm/FishingStateMachineTests.cs`
- `tests/FishingBot.Tests/Config/NormalizedRectTests.cs`
- `tests/FishingBot.Tests/Vision/StartPromptDetectorTests.cs`
- `tests/FishingBot.Tests/Vision/TensionDetectorTests.cs`
- `tests/FishingBot.Tests/Vision/CatchMenuDetectorTests.cs`

---

### Task 1: Bootstrap solution and dependencies

**Files:**
- Create: `FishingBot.sln`, all `.csproj` above

- [ ] **Step 1: Create solution and projects**

```powershell
dotnet new sln -n FishingBot
dotnet new wpf -n FishingBot.App -o src/FishingBot.App -f net8.0-windows
dotnet new classlib -n FishingBot.Core -o src/FishingBot.Core -f net8.0
dotnet new xunit -n FishingBot.Tests -o tests/FishingBot.Tests -f net8.0
dotnet sln FishingBot.sln add src/FishingBot.App/FishingBot.App.csproj src/FishingBot.Core/FishingBot.Core.csproj tests/FishingBot.Tests/FishingBot.Tests.csproj
dotnet add src/FishingBot.App/FishingBot.App.csproj reference src/FishingBot.Core/FishingBot.Core.csproj
dotnet add tests/FishingBot.Tests/FishingBot.Tests.csproj reference src/FishingBot.Core/FishingBot.Core.csproj
```

- [ ] **Step 2: Add NuGet packages**

```powershell
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package SharpDX
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package SharpDX.DXGI
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package SharpDX.Direct3D11
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package OpenCvSharp4
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package OpenCvSharp4.runtime.win
dotnet add src/FishingBot.Core/FishingBot.Core.csproj package Newtonsoft.Json
dotnet add src/FishingBot.App/FishingBot.App.csproj package Newtonsoft.Json
```

- [ ] **Step 3: Run restore/build**

Run: `dotnet build FishingBot.sln -c Debug`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "chore: initialize FishingBot solution and dependencies"
```

---

### Task 2: Add config model and normalized ROI conversion (TDD)

**Files:**
- Create: `src/FishingBot.Core/Config/NormalizedRect.cs`, `src/FishingBot.Core/Config/BotConfig.cs`, `src/FishingBot.Core/Config/ConfigService.cs`, `config.json`
- Test: `tests/FishingBot.Tests/Config/NormalizedRectTests.cs`

- [ ] **Step 1: Write failing test for normalized ROI**

```csharp
using FishingBot.Core.Config;
using Xunit;

namespace FishingBot.Tests.Config;

public class NormalizedRectTests
{
    [Fact]
    public void ToPixelRect_ConvertsNormalizedToPixels()
    {
        var rect = new NormalizedRect(0.5, 0.25, 0.1, 0.2);
        var px = rect.ToPixelRect(2560, 1440);

        Assert.Equal(1280, px.X);
        Assert.Equal(360, px.Y);
        Assert.Equal(256, px.Width);
        Assert.Equal(288, px.Height);
    }
}
```

- [ ] **Step 2: Run test to verify fail**

Run: `dotnet test tests/FishingBot.Tests/FishingBot.Tests.csproj --filter "FullyQualifiedName~NormalizedRectTests"`
Expected: FAIL (type/method missing)

- [ ] **Step 3: Implement config classes**

```csharp
// src/FishingBot.Core/Config/NormalizedRect.cs
using System.Drawing;

namespace FishingBot.Core.Config;

public readonly record struct NormalizedRect(double X, double Y, double W, double H)
{
    public Rectangle ToPixelRect(int screenW, int screenH)
        => new((int)(X * screenW), (int)(Y * screenH), (int)(W * screenW), (int)(H * screenH));
}
```

```csharp
// src/FishingBot.Core/Config/BotConfig.cs
namespace FishingBot.Core.Config;

public sealed class BotConfig
{
    public string Mode { get; set; } = "DetectOnly";
    public string FishAction { get; set; } = "RELEASE";
    public RegionConfig Regions { get; set; } = new();
    public TimingConfig Timing { get; set; } = new();
}

public sealed class RegionConfig
{
    public NormalizedRect StartPrompt { get; set; } = new(0.01, 0.01, 0.35, 0.12);
    public NormalizedRect TensionWidget { get; set; } = new(0.69, 0.67, 0.20, 0.26);
    public NormalizedRect FightBar { get; set; } = new(0.30, 0.78, 0.45, 0.16);
    public NormalizedRect CatchMenu { get; set; } = new(0.35, 0.20, 0.32, 0.58);
}

public sealed class TimingConfig
{
    public int AimCheckIntervalMs { get; set; } = 16;
    public int TensionCheckIntervalMs { get; set; } = 50;
    public int FightCheckIntervalMs { get; set; } = 50;
    public int ActionDelayMin { get; set; } = 80;
    public int ActionDelayMax { get; set; } = 200;
}
```

- [ ] **Step 4: Add ConfigService with load/save**

```csharp
// src/FishingBot.Core/Config/ConfigService.cs
using Newtonsoft.Json;

namespace FishingBot.Core.Config;

public sealed class ConfigService
{
    public BotConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<BotConfig>(json) ?? new BotConfig();
    }

    public void Save(string path, BotConfig config)
    {
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/FishingBot.Tests/FishingBot.Tests.csproj`
Expected: PASS for config tests

- [ ] **Step 6: Commit**

```bash
git add src/FishingBot.Core/Config tests/FishingBot.Tests/Config config.json
git commit -m "feat: add normalized ROI config and json persistence"
```

---

### Task 3: Implement FSM with StartPrompt stage (TDD)

**Files:**
- Create: `src/FishingBot.Core/Fsm/FishingState.cs`, `src/FishingBot.Core/Fsm/FishingEvent.cs`, `src/FishingBot.Core/Fsm/FishingStateMachine.cs`, `src/FishingBot.Core/Fsm/StateTimeouts.cs`
- Test: `tests/FishingBot.Tests/Fsm/FishingStateMachineTests.cs`

- [ ] **Step 1: Write failing FSM transition tests**

```csharp
using FishingBot.Core.Fsm;
using Xunit;

namespace FishingBot.Tests.Fsm;

public class FishingStateMachineTests
{
    [Fact]
    public void StartPromptDetected_MovesToStartFishing()
    {
        var fsm = new FishingStateMachine();
        fsm.Handle(FishingEvent.StartPromptDetected);
        Assert.Equal(FishingState.StartFishing, fsm.Current);
    }

    [Fact]
    public void BiteDetected_MovesToHook_FromWaitBite()
    {
        var fsm = new FishingStateMachine(FishingState.WaitBite);
        fsm.Handle(FishingEvent.BiteDetected);
        Assert.Equal(FishingState.Hook, fsm.Current);
    }
}
```

- [ ] **Step 2: Run tests to verify fail**

Run: `dotnet test tests/FishingBot.Tests/FishingBot.Tests.csproj --filter "FullyQualifiedName~FishingStateMachineTests"`
Expected: FAIL

- [ ] **Step 3: Implement states/events and reducer**

```csharp
// FishingState.cs
namespace FishingBot.Core.Fsm;

public enum FishingState
{
    Idle,
    WaitStartPrompt,
    StartFishing,
    WaitBite,
    Hook,
    Fight,
    CatchMenu,
    ApplyAction,
    Recast
}
```

```csharp
// FishingEvent.cs
namespace FishingBot.Core.Fsm;

public enum FishingEvent
{
    StartPromptDetected,
    StartFishingDone,
    BiteDetected,
    HookDone,
    FightDetected,
    CatchMenuDetected,
    ActionApplied,
    RecastDone,
    Timeout,
    Reset
}
```

- [ ] **Step 4: Implement transition logic and timeouts**

```csharp
// FishingStateMachine.cs
namespace FishingBot.Core.Fsm;

public sealed class FishingStateMachine
{
    public FishingState Current { get; private set; }

    public FishingStateMachine(FishingState initial = FishingState.WaitStartPrompt)
        => Current = initial;

    public void Handle(FishingEvent evt)
    {
        Current = (Current, evt) switch
        {
            (FishingState.WaitStartPrompt, FishingEvent.StartPromptDetected) => FishingState.StartFishing,
            (FishingState.StartFishing, FishingEvent.StartFishingDone) => FishingState.WaitBite,
            (FishingState.WaitBite, FishingEvent.BiteDetected) => FishingState.Hook,
            (FishingState.Hook, FishingEvent.HookDone) => FishingState.Fight,
            (FishingState.Fight, FishingEvent.CatchMenuDetected) => FishingState.CatchMenu,
            (FishingState.CatchMenu, FishingEvent.ActionApplied) => FishingState.Recast,
            (FishingState.Recast, FishingEvent.RecastDone) => FishingState.WaitStartPrompt,
            (_, FishingEvent.Timeout) => FishingState.WaitStartPrompt,
            (_, FishingEvent.Reset) => FishingState.WaitStartPrompt,
            _ => Current
        };
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/FishingBot.Tests/FishingBot.Tests.csproj --filter "FullyQualifiedName~FishingStateMachineTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FishingBot.Core/Fsm tests/FishingBot.Tests/Fsm
git commit -m "feat: implement fishing FSM with start prompt stage"
```

---

### Task 4: Build input engine (E/Space/A/D + safety release)

**Files:**
- Create: `src/FishingBot.Core/Input/ScanCodes.cs`, `src/FishingBot.Core/Input/SendInputNative.cs`, `src/FishingBot.Core/Input/InputEngine.cs`, `src/FishingBot.Core/Input/BezierMouseMover.cs`

- [ ] **Step 1: Add scan-code map**

```csharp
namespace FishingBot.Core.Input;

public static class ScanCodes
{
    public const ushort E = 0x12;
    public const ushort Space = 0x39;
    public const ushort A = 0x1E;
    public const ushort D = 0x20;
}
```

- [ ] **Step 2: Implement SendInput wrapper and key up/down**

Run: `dotnet build src/FishingBot.Core/FishingBot.Core.csproj`
Expected: wrapper compiles with P/Invoke structs

- [ ] **Step 3: Implement InputEngine actions**

Methods required:
- `PressE()`
- `PressSpace()`
- `HoldA()` / `HoldD()`
- `ReleaseAD()`
- `ReleaseAll()`
- `ClickAt(int x, int y)` (через BezierMover)

- [ ] **Step 4: Manual safety check**

Run app in test mode and verify:
- Panic делает `ReleaseAll()`
- Stop делает `ReleaseAll()`

- [ ] **Step 5: Commit**

```bash
git add src/FishingBot.Core/Input
git commit -m "feat: add scan-code input engine with emergency key release"
```

---

### Task 5: Implement vision detectors (StartPrompt, Tension, Fight, CatchMenu)

**Files:**
- Create: `src/FishingBot.Core/Vision/*.cs`
- Test: `tests/FishingBot.Tests/Vision/*.cs`

- [ ] **Step 1: Add detector contract and snapshot model**

```csharp
namespace FishingBot.Core.Vision;

public sealed record VisionSnapshot(
    bool StartPromptDetected,
    double StartPromptConfidence,
    bool BiteDetected,
    double BiteConfidence,
    bool FightDetected,
    int FightMarkerX,
    bool CatchMenuDetected,
    double CatchMenuConfidence);
```

- [ ] **Step 2: Write failing test for StartPromptDetector**

```csharp
[Fact]
public void StartPromptDetector_ReturnsFalse_OnEmptyFrame()
{
    var d = new StartPromptDetector();
    using var mat = new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.Black);
    var r = d.Detect(mat);
    Assert.False(r.IsDetected);
}
```

- [ ] **Step 3: Implement StartPromptDetector (ROI + threshold + N confirms)**

Run: `dotnet test tests/FishingBot.Tests/FishingBot.Tests.csproj --filter "FullyQualifiedName~StartPromptDetectorTests"`
Expected: PASS

- [ ] **Step 4: Implement TensionDetector (red bar rule)**

Detection rule for bite:
- `avgR > 150 && avgG < 90 && avgB < 90`

- [ ] **Step 5: Implement FightDetector (bar exists + marker X)**

Output:
- `FightDetected`
- `FightMarkerX`

- [ ] **Step 6: Implement CatchMenuDetector (block + template match buttons)**

Use templates:
- `templates/fishing/btn_take.png`
- `templates/fishing/btn_release.png`

- [ ] **Step 7: Commit**

```bash
git add src/FishingBot.Core/Vision tests/FishingBot.Tests/Vision templates/fishing
git commit -m "feat: add vision detectors for start prompt, bite, fight and catch menu"
```

---

### Task 6: Implement DXGI capture engine and frame pipeline

**Files:**
- Create: `src/FishingBot.Core/Capture/CapturedFrame.cs`, `src/FishingBot.Core/Capture/DxgiCaptureEngine.cs`, `src/FishingBot.Core/Contracts/ICaptureEngine.cs`

- [ ] **Step 1: Add `ICaptureEngine` contract**

```csharp
public interface ICaptureEngine : IDisposable
{
    bool TryGetLatestFrame(out CapturedFrame frame);
}
```

- [ ] **Step 2: Implement DXGI duplication init**

Run: `dotnet build src/FishingBot.Core/FishingBot.Core.csproj`
Expected: `Build succeeded.`

- [ ] **Step 3: Implement frame extraction to Mat-friendly buffer**

Requirements:
- 60fps target attempt (best effort)
- no UI thread blocking

- [ ] **Step 4: Add graceful failure handling**

Handle:
- lost duplication access
- temporary acquire timeout

- [ ] **Step 5: Commit**

```bash
git add src/FishingBot.Core/Capture src/FishingBot.Core/Contracts/ICaptureEngine.cs
git commit -m "feat: add DXGI capture engine with safe frame acquisition"
```

---

### Task 7: Wire orchestrator (DetectOnly and Active modes)

**Files:**
- Create: `src/FishingBot.Core/Orchestration/AppOrchestrator.cs`, `src/FishingBot.Core/Contracts/*.cs`, `src/FishingBot.Core/Logging/*.cs`

- [ ] **Step 1: Add orchestrator loop skeleton**

Loop sequence:
1. read frame
2. build vision snapshot
3. update FSM
4. if Active -> execute input action
5. push telemetry/log

- [ ] **Step 2: Implement DetectOnly guard**

Rule:
- In `DetectOnly` no call to `IInputEngine` methods except `ReleaseAll()` on stop/panic.

- [ ] **Step 3: Implement state actions**

Actions map:
- `StartFishing` -> `PressE()`
- `Hook` -> `PressSpace()`
- `Fight` -> hold/switch A/D by `deltaX`
- `ApplyAction` -> click TAKE/RELEASE button

- [ ] **Step 4: Add timeout guards**

On timeout in any phase:
- `ReleaseAll()`
- FSM reset to `WaitStartPrompt`

- [ ] **Step 5: Commit**

```bash
git add src/FishingBot.Core/Orchestration src/FishingBot.Core/Contracts src/FishingBot.Core/Logging
git commit -m "feat: orchestrate vision-fsm-input pipeline with detect-only safety mode"
```

---

### Task 8: Build WPF GUI (MVVM + live debug + calibration)

**Files:**
- Create/Modify: `src/FishingBot.App/MainWindow.xaml`, `src/FishingBot.App/ViewModels/MainViewModel.cs`, `src/FishingBot.App/ViewModels/CalibrationViewModel.cs`, `src/FishingBot.App/MainWindow.xaml.cs`

- [ ] **Step 1: Build basic controls and commands**

Controls:
- Start DetectOnly
- Start Active
- Pause
- Stop
- FishAction TAKE/RELEASE

- [ ] **Step 2: Bind live telemetry**

Show:
- CurrentState
- StartPromptDetected
- Bite/Fight/Catch confidences
- fps capture/vision

- [ ] **Step 3: Add calibration editor for normalized ROI**

Capabilities:
- select ROI profile
- move/resize
- save to config

- [ ] **Step 4: Add logs panel**

Display last 500 log rows with timestamps.

- [ ] **Step 5: Commit**

```bash
git add src/FishingBot.App
git commit -m "feat: add WPF control panel with live debug and ROI calibration"
```

---

### Task 9: Add global hotkeys and panic/cleanup behavior

**Files:**
- Modify: `src/FishingBot.App/MainWindow.xaml.cs`, `src/FishingBot.Core/Orchestration/AppOrchestrator.cs`, `src/FishingBot.Core/Input/InputEngine.cs`

- [ ] **Step 1: Register F9/F10 hotkeys**

Rules:
- F9: Pause/Resume
- F10: Panic Stop

- [ ] **Step 2: Implement panic flow**

Panic flow:
1. stop loops
2. `ReleaseAll()`
3. FSM reset
4. log `PANIC_TRIGGERED`

- [ ] **Step 3: Manual verification**

Scenario:
- trigger panic during fight while A/D held
- expected: immediate release + no stuck input

- [ ] **Step 4: Commit**

```bash
git add src/FishingBot.App/MainWindow.xaml.cs src/FishingBot.Core/Orchestration src/FishingBot.Core/Input
git commit -m "feat: add global pause and panic hotkeys with safe shutdown"
```

---

### Task 10: Final tests, docs, and runbook

**Files:**
- Create/Modify: `README.md`, `tests/FishingBot.Tests/*`, `config.json`

- [ ] **Step 1: Run unit and integration tests**

Run: `dotnet test FishingBot.sln -c Debug`
Expected: all tests PASS

- [ ] **Step 2: Build release**

Run: `dotnet build FishingBot.sln -c Release`
Expected: `Build succeeded.`

- [ ] **Step 3: Write README sections**

Include:
- prerequisites
- install/build
- DetectOnly calibration flow
- Active mode flow
- known limitations
- safety notes (pause/panic)

- [ ] **Step 4: Final commit**

```bash
git add README.md tests config.json
git commit -m "docs: add setup, calibration, and operation guide for FishingBot MVP"
```

---

## Verification Checklist (before implementation completion)

- [ ] Приложение запускается и GUI отображает live-телеметрию.
- [ ] StartPromptDetector уверенно детектит «Нажмите E чтобы начать рыбалку».
- [ ] В Active корректно отправляется `E` на старте рыбалки.
- [ ] Bite detection переводит в Hook и отправляет `Space`.
- [ ] Fight logic переключает A/D по движению маркера.
- [ ] Catch menu детектится, кнопка TAKE/RELEASE кликается по конфигу.
- [ ] Recast возвращает цикл в WaitStartPrompt.
- [ ] F9/F10 всегда работают, залипания клавиш нет.

---

## Self-Review

1. **Spec coverage:** покрыты все обязательные этапы из спецификации, включая новый стартовый этап `WAIT_START_PROMPT` и действие `E`.
2. **Placeholder scan:** в плане нет TBD/TODO и ссылок «сделать аналогично».
3. **Type consistency:** состояния/события/имена детекторов и режимов синхронизированы с design doc.
