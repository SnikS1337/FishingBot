namespace FishingBot.Core.Config;

public sealed class BotConfig
{
    public ResolutionConfig Resolution { get; set; } = new();

    public FishingConfig Fishing { get; set; } = new();

    public HotkeysConfig Hotkeys { get; set; } = new();

    public TimingConfig Timing { get; set; } = new();

    public RegionsConfig Regions { get; set; } = new();

    public DetectionConfig Detection { get; set; } = new();
}

public sealed class ResolutionConfig
{
    public int W { get; set; } = 2560;

    public int H { get; set; } = 1440;
}

public sealed class FishingConfig
{
    public string Action { get; set; } = "RELEASE";

    public bool AutoRecast { get; set; } = true;

    public int[] RecastDelayMs { get; set; } = [1000, 3000];
}

public sealed class HotkeysConfig
{
    public string Panic { get; set; } = "F10";

    public string Pause { get; set; } = "F9";
}

public sealed class TimingConfig
{
    public int StartPromptCheckIntervalMs { get; set; } = 50;

    public int TensionCheckIntervalMs { get; set; } = 50;

    public int FightCheckIntervalMs { get; set; } = 50;

    public int ActionDelayMin { get; set; } = 80;

    public int ActionDelayMax { get; set; } = 200;
}

public sealed class RegionsConfig
{
    public NormalizedRect StartPrompt { get; set; } = new(0.01, 0.01, 0.35, 0.12);

    public NormalizedRect TensionWidget { get; set; } = new(0.69, 0.67, 0.20, 0.26);

    public NormalizedRect FightBar { get; set; } = new(0.30, 0.78, 0.45, 0.16);

    public NormalizedRect CatchMenu { get; set; } = new(0.35, 0.20, 0.32, 0.58);
}

public sealed class DetectionConfig
{
    public double StartPromptThreshold { get; set; } = 0.80;

    public int StartPromptConfirmFrames { get; set; } = 2;

    public double BiteThreshold { get; set; } = 0.75;

    public int BiteConfirmFrames { get; set; } = 2;

    public double CatchMenuThreshold { get; set; } = 0.80;

    public int CatchMenuConfirmFrames { get; set; } = 2;
}
