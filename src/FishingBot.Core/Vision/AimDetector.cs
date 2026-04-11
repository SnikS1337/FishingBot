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

        using var barMask = new Mat();
        Cv2.InRange(hsv, new Scalar(0, 0, 10), new Scalar(180, 80, 80), barMask);

        using var brightMask = new Mat();
        Cv2.InRange(hsv, new Scalar(0, 0, 220), new Scalar(180, 60, 255), brightMask);

        var greenStats = FindHorizontalBounds(greenMask, minPixels: 10, minWidth: 6);
        var sliderX = FindBrightSliderX(brightMask);

        if (!greenStats.HasValue || sliderX < 0)
        {
            return new DetectionResult(false, 0);
        }

        var green = greenStats.Value;
        var barBounds = FindBarBounds(barMask, green.left, green.right);

        if (!barBounds.HasValue || sliderX < barBounds.Value.left || sliderX > barBounds.Value.right)
        {
            return new DetectionResult(false, 0);
        }

        var (left, right, ratio) = green;
        var inZone = sliderX >= left && sliderX <= right;

        var zoneCenter = (left + right) / 2.0;
        var zoneHalf = Math.Max(1.0, (right - left) / 2.0);
        var distance = Math.Abs(sliderX - zoneCenter);
        var proximity = 1.0 - Math.Clamp(distance / zoneHalf, 0.0, 1.0);
        var confidence = Math.Clamp((proximity * 0.8) + (Math.Min(ratio * 8.0, 1.0) * 0.2), 0.0, 1.0);

        return new DetectionResult(inZone, confidence, sliderX);
    }

    private static (int left, int right, double ratio)? FindHorizontalBounds(Mat mask, int minPixels, int minWidth)
    {
        if (mask.Empty())
        {
            return null;
        }

        var pixels = Cv2.CountNonZero(mask);
        if (pixels < minPixels)
        {
            return null;
        }

        var left = -1;
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

        var right = -1;
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

        if (left < 0 || right <= left || (right - left + 1) < minWidth)
        {
            return null;
        }

        return (left, right, pixels / (double)(mask.Rows * mask.Cols));
    }

    private static (int left, int right)? FindBarBounds(Mat barMask, int greenLeft, int greenRight)
    {
        var left = -1;
        for (var x = 0; x < greenLeft; x++)
        {
            using var col = barMask.Col(x);
            if (Cv2.CountNonZero(col) > 0)
            {
                left = x;
                break;
            }
        }

        var right = -1;
        for (var x = barMask.Cols - 1; x > greenRight; x--)
        {
            using var col = barMask.Col(x);
            if (Cv2.CountNonZero(col) > 0)
            {
                right = x;
                break;
            }
        }

        return left >= 0 && right > greenRight ? (left, right) : null;
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
