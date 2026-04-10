using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class TensionDetector
{
    public DetectionResult Detect(Mat frame)
    {
        if (frame.Empty())
        {
            return new DetectionResult(false, 0);
        }

        var barHeight = Math.Max(1, (int)(frame.Rows * 0.20));
        var barY = Math.Max(0, frame.Rows - barHeight);
        using var barRoi = new Mat(frame, new OpenCvSharp.Rect(0, barY, frame.Cols, barHeight));

        var mean = Cv2.Mean(barRoi);
        var b = mean.Val0;
        var g = mean.Val1;
        var r = mean.Val2;

        var isRed = r > 150 && g < 90 && b < 90;
        var confidence = Math.Clamp((r - Math.Max(g, b)) / 255.0, 0.0, 1.0);

        return new DetectionResult(isRed, confidence);
    }
}
