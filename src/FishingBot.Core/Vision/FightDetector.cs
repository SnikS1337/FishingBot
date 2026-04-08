using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class FightDetector
{
    public DetectionResult Detect(Mat frame)
    {
        if (frame.Empty())
        {
            return new DetectionResult(false, 0, -1);
        }

        using var hsv = new Mat();
        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

        using var yellowMask = new Mat();
        Cv2.InRange(hsv, new Scalar(18, 80, 80), new Scalar(40, 255, 255), yellowMask);

        var yellowPixels = Cv2.CountNonZero(yellowMask);
        var totalPixels = frame.Rows * frame.Cols;
        if (totalPixels <= 0)
        {
            return new DetectionResult(false, 0, -1);
        }

        var yellowRatio = yellowPixels / (double)totalPixels;
        var fightDetected = yellowRatio > 0.03;

        // Простейший поиск светлого маркера по X (сумма яркости по колонкам)
        using var gray = new Mat();
        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        using var markerBinary = new Mat();
        Cv2.Threshold(gray, markerBinary, 225, 255, ThresholdTypes.Binary);

        var markerX = -1;
        var maxColumn = 0;
        for (var x = 0; x < markerBinary.Cols; x++)
        {
            var column = markerBinary.Col(x);
            var value = Cv2.CountNonZero(column);
            if (value > maxColumn)
            {
                maxColumn = value;
                markerX = x;
            }
        }

        var confidence = Math.Min(1.0, yellowRatio * 5.0);
        return new DetectionResult(fightDetected, confidence, markerX);
    }
}
