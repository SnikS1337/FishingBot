using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class AimDetector
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
        Cv2.InRange(hsv, new Scalar(60, 80, 80), new Scalar(150, 255, 255), greenMask);

        using var brightMask = new Mat();
        Cv2.InRange(hsv, new Scalar(0, 0, 220), new Scalar(180, 60, 255), brightMask);

        var greenStats = FindHorizontalBounds(greenMask);
        var sliderX = FindBrightSliderX(brightMask);

        if (!greenStats.HasValue || sliderX < 0)
        {
            return new DetectionResult(false, 0);
        }

        var (left, right, ratio) = greenStats.Value;
        var inZone = sliderX >= left && sliderX <= right;

        var zoneCenter = (left + right) / 2.0;
        var zoneHalf = Math.Max(1.0, (right - left) / 2.0);
        var distance = Math.Abs(sliderX - zoneCenter);
        var proximity = 1.0 - Math.Clamp(distance / zoneHalf, 0.0, 1.0);
        var confidence = Math.Clamp((proximity * 0.8) + (Math.Min(ratio * 8.0, 1.0) * 0.2), 0.0, 1.0);

        return new DetectionResult(inZone, confidence, sliderX);
    }

    private static (int left, int right, double ratio)? FindHorizontalBounds(Mat mask)
    {
        var total = mask.Rows * mask.Cols;
        if (total <= 0)
        {
            return null;
        }

        var greenPixels = Cv2.CountNonZero(mask);
        if (greenPixels < 10)
        {
            return null;
        }

        var left = -1;
        var right = -1;
        for (var x = 0; x < mask.Cols; x++)
        {
            using var col = mask.Col(x);
            if (Cv2.CountNonZero(col) <= 0)
            {
                continue;
            }

            left = x;
            break;
        }

        for (var x = mask.Cols - 1; x >= 0; x--)
        {
            using var col = mask.Col(x);
            if (Cv2.CountNonZero(col) <= 0)
            {
                continue;
            }

            right = x;
            break;
        }

        if (left < 0 || right <= left)
        {
            return null;
        }

        return (left, right, greenPixels / (double)total);
    }

    private static int FindBrightSliderX(Mat brightMask)
    {
        var bestX = -1;
        var best = 0;

        for (var x = 0; x < brightMask.Cols; x++)
        {
            using var col = brightMask.Col(x);
            var value = Cv2.CountNonZero(col);
            if (value <= best)
            {
                continue;
            }

            best = value;
            bestX = x;
        }

        return best < 2 ? -1 : bestX;
    }
}
