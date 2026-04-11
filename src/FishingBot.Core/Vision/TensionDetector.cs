using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class TensionDetector
{
    private const double MinimumLocalizedRedAreaRatio = 0.03;

    public DetectionResult Detect(Mat frame)
    {
        if (frame is null || frame.Empty())
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

        Cv2.FindContours(
            redMask,
            out var contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        var largestContourArea = 0.0;
        foreach (var contour in contours)
        {
            var contourArea = Cv2.ContourArea(contour);
            if (contourArea > largestContourArea)
            {
                largestContourArea = contourArea;
            }
        }

        var localizedRedAreaRatio = largestContourArea / totalPixels;
        var isRed = localizedRedAreaRatio >= MinimumLocalizedRedAreaRatio;
        var confidence = Math.Clamp(localizedRedAreaRatio / MinimumLocalizedRedAreaRatio, 0.0, 1.0);
        return new DetectionResult(isRed, confidence);
    }
}
