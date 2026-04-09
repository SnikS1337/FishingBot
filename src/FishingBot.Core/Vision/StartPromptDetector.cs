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

        using var hsv = new Mat();
        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

        using var greenMask = new Mat();
        Cv2.InRange(hsv, new Scalar(45, 80, 80), new Scalar(95, 255, 255), greenMask);

        using var greenDilated = new Mat();
        Cv2.Dilate(greenMask, greenDilated, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));

        var greenPixels = Cv2.CountNonZero(greenDilated);
        var totalPixels = frame.Rows * frame.Cols;
        if (greenPixels < 8 || totalPixels <= 0)
        {
            return new DetectionResult(false, 0);
        }

        var greenRatio = greenPixels / (double)totalPixels;

        // Ищем достаточно компактный зеленый маркер (галочка/иконка)
        Cv2.FindContours(greenDilated, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        if (contours.Length == 0)
        {
            return new DetectionResult(false, 0);
        }

        var maxArea = 0.0;
        var maxRect = new Rect();
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area <= maxArea)
            {
                continue;
            }

            maxArea = area;
            maxRect = Cv2.BoundingRect(contour);
        }

        if (maxArea < 20)
        {
            return new DetectionResult(false, 0);
        }

        var compactness = maxRect.Width > 0 && maxRect.Height > 0
            ? Math.Min(maxRect.Width, maxRect.Height) / (double)Math.Max(maxRect.Width, maxRect.Height)
            : 0;

        var confidence = Math.Clamp((greenRatio * 10.0 * 0.5) + (compactness * 0.5), 0.0, 1.0);
        var detected = greenRatio is > 0.001 and < 0.18 && compactness > 0.20;
        return new DetectionResult(detected, confidence);
    }
}
