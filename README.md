# FishingBot (MVP)

FishingBot - desktop automation helper for fishing flow with DXGI capture, OpenCV analysis, state machine control, and WPF control panel.

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- GPU/driver support for DXGI Desktop Duplication

## Quick Start

```powershell
dotnet restore FishingBot.sln
dotnet build FishingBot.sln -c Debug
dotnet run --project src/FishingBot.App/FishingBot.App.csproj -c Debug
```

## Current Capabilities

- Full state flow: `WaitStartPrompt -> StartFishing(E) -> WaitBite -> Hook(Space) -> Fight(A/D) -> CatchMenu -> Recast`
- Capture: DXGI Desktop Duplication (`SharpDX`)
- Vision: OpenCV detectors for start prompt, tension, fight bar, catch menu
- Input: `SendInput` scan-codes + Bezier mouse movement
- Safety: DetectOnly mode, Pause/Resume (`F9`), Panic Stop (`F10`)
- GUI: live telemetry, ROI config fields, debug logs, live preview with ROI overlays

## DetectOnly Calibration Flow

1. Launch app and click `Start DetectOnly`
2. Tune normalized ROI fields in Calibration panel
3. Click `Save Config`
4. Verify detections via telemetry + logs + live preview overlays
5. Stop DetectOnly mode

## Active Mode Flow

1. Select fish action: `TAKE` or `RELEASE`
2. Click `Start Active`
3. Monitor state transitions and detector confidence in telemetry
4. Use `F9` to pause/resume, `F10` for immediate panic stop

## Config

Main config file: `config.json`

Includes:
- normalized regions (`startPrompt`, `tensionWidget`, `fightBar`, `catchMenu`)
- timing intervals and action delays
- detection thresholds
- hotkeys and post-catch action

## Templates

Optional button templates (recommended for stronger catch menu detection):

- `templates/fishing/btn_take.png`
- `templates/fishing/btn_release.png`

If templates are missing, detector works in heuristic mode.

## Safety Notes

- Run first in `DetectOnly` mode and validate all regions.
- Keep `F10` ready for panic stop.
- Do not run with incorrect resolution/UI layout without recalibration.

## Known Limitations (MVP)

- No drag/resize overlay editor yet (ROI tuning is field-based).
- Detector thresholds are heuristic and may require profile tuning per server/UI scale.
- SharpDX is legacy; future migration to maintained DX capture wrapper is recommended.
