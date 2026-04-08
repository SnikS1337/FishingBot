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

        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 210, 255, ThresholdTypes.Binary);

        var brightPixels = Cv2.CountNonZero(binary);
        var totalPixels = frame.Rows * frame.Cols;
        if (totalPixels <= 0)
        {
            return new DetectionResult(false, 0);
        }

        var ratio = brightPixels / (double)totalPixels;
        var confidence = Math.Min(1.0, ratio * 8.0);

        // Эвристика для UI-плашки/текста в левом верхнем углу
        var detected = ratio is > 0.07 and < 0.55;
        return new DetectionResult(detected, confidence);
    }
}
