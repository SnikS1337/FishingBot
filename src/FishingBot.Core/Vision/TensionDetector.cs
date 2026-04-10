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

        var barHeight = Math.Max(1, (int)(frame.Rows * 0.35));
        var barY = Math.Max(0, frame.Rows - barHeight);
        using var barRoi = new Mat(frame, new OpenCvSharp.Rect(0, barY, frame.Cols, barHeight));

        using var hsv = new Mat();
        Cv2.CvtColor(barRoi, hsv, ColorConversionCodes.BGR2HSV);

        using var redMaskLow = new Mat();
        using var redMaskHigh = new Mat();
        using var redMask = new Mat();

        Cv2.InRange(hsv, new Scalar(0, 90, 90), new Scalar(12, 255, 255), redMaskLow);
        Cv2.InRange(hsv, new Scalar(165, 90, 90), new Scalar(180, 255, 255), redMaskHigh);
        Cv2.BitwiseOr(redMaskLow, redMaskHigh, redMask);

        var totalPixels = redMask.Rows * redMask.Cols;
        if (totalPixels <= 0)
        {
            return new DetectionResult(false, 0);
        }

        var redPixels = Cv2.CountNonZero(redMask);
        var redRatio = redPixels / (double)totalPixels;

        var isRed = redRatio > 0.02;
        var confidence = Math.Clamp(redRatio * 10.0, 0.0, 1.0);
        return new DetectionResult(isRed, confidence);
    }
}
