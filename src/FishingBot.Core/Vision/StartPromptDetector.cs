using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class StartPromptDetector
{
    public DetectionResult Detect(Mat frame)
    {
        if (frame.Empty())
        {
            return new DetectionResult(false, 0);
        }

        using var gray = new Mat();
        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        using var brightMask = new Mat();
        Cv2.Threshold(gray, brightMask, 205, 255, ThresholdTypes.Binary);

        using var darkMask = new Mat();
        Cv2.Threshold(gray, darkMask, 65, 255, ThresholdTypes.BinaryInv);

        var totalPixels = frame.Rows * frame.Cols;
        if (totalPixels <= 0)
        {
            return new DetectionResult(false, 0);
        }

        var brightPixels = Cv2.CountNonZero(brightMask);
        var darkPixels = Cv2.CountNonZero(darkMask);
        var brightRatio = brightPixels / (double)totalPixels;
        var darkRatio = darkPixels / (double)totalPixels;

        // Ищем контур, похожий на клавишу E (светлый прямоугольник около квадратного).
        Cv2.FindContours(brightMask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var hasEKeyLikeRect = false;
        var bestArea = 0.0;
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < 12)
            {
                continue;
            }

            var rect = Cv2.BoundingRect(contour);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            var aspect = rect.Width / (double)rect.Height;
            if (aspect is > 0.65 and < 1.45)
            {
                hasEKeyLikeRect = true;
                bestArea = Math.Max(bestArea, area);
            }
        }

        var areaConfidence = Math.Clamp(bestArea / 250.0, 0.0, 1.0);
        var contrastConfidence = Math.Clamp((brightRatio * 8.0) + (darkRatio * 0.8), 0.0, 1.0);
        var confidence = (areaConfidence * 0.6) + (contrastConfidence * 0.4);

        var detected = hasEKeyLikeRect
            && brightRatio is > 0.004 and < 0.30
            && darkRatio > 0.25;

        return new DetectionResult(detected, confidence);
    }
}
