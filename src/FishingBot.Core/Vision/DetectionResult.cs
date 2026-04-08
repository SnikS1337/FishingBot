using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed record DetectionResult(
    bool IsDetected,
    double Confidence,
    int MarkerX = -1,
    Point? ButtonTake = null,
    Point? ButtonRelease = null);
