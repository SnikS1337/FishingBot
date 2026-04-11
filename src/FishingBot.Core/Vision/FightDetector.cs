using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class FightDetector
{
    private static readonly Scalar MinimumFightBarHsv = new(18, 80, 80);
    private static readonly Scalar MaximumFightBarHsv = new(40, 255, 255);

    private const double MinimumBarWidthRatio = 0.25;
    private const int MinimumAbsoluteBarWidthPixels = 12;
    private const int MinimumBarHeightPixels = 2;
    private const double MinimumBarAspectRatio = 4.0;

    private const byte MinimumMarkerBrightness = 225;
    private const int MinimumMarkerColumnPixels = 2;
    private const int MarkerSearchHeightScale = 6;
    private const int MinimumMarkerSearchHeightPixels = 12;

    private const double BaseDetectionConfidence = 0.35;
    private const double BarCoverageConfidenceWeight = 0.45;

    public DetectionResult Detect(Mat frame)
    {
        if (frame is null || frame.Empty())
        {
            return new DetectionResult(false, 0, -1);
        }

        using var hsv = new Mat();
        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

        using var yellowMask = new Mat();
        Cv2.InRange(hsv, MinimumFightBarHsv, MaximumFightBarHsv, yellowMask);

        var fightBarBounds = FindFightBarBounds(yellowMask);
        if (!fightBarBounds.HasValue)
        {
            return new DetectionResult(false, 0, -1);
        }

        using var gray = new Mat();
        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        using var markerBinary = new Mat();
        Cv2.Threshold(gray, markerBinary, MinimumMarkerBrightness, 255, ThresholdTypes.Binary);

        var fightBar = fightBarBounds.Value;
        var markerX = FindMarkerX(markerBinary, fightBar);
        if (markerX < 0)
        {
            return new DetectionResult(false, 0, -1);
        }

        var barCoverage = Math.Clamp(fightBar.Width / (double)Math.Max(1, frame.Cols), 0.0, 1.0);
        var confidence = Math.Clamp(BaseDetectionConfidence + (barCoverage * BarCoverageConfidenceWeight), 0.0, 1.0);
        return new DetectionResult(true, confidence, markerX);
    }

    private static Rect? FindFightBarBounds(Mat yellowMask)
    {
        if (yellowMask.Empty())
        {
            return null;
        }

        var minimumWidth = Math.Max(MinimumAbsoluteBarWidthPixels, (int)Math.Round(yellowMask.Cols * MinimumBarWidthRatio));
        var left = -1;
        var right = -1;
        var occupiedColumns = 0;

        for (var x = 0; x < yellowMask.Cols; x++)
        {
            using var column = yellowMask.Col(x);
            if (Cv2.CountNonZero(column) <= 0)
            {
                continue;
            }

            occupiedColumns++;

            left = x;
            break;
        }

        for (var x = Math.Max(0, left + 1); x < yellowMask.Cols; x++)
        {
            using var column = yellowMask.Col(x);
            if (Cv2.CountNonZero(column) > 0)
            {
                occupiedColumns++;
            }
        }

        for (var x = yellowMask.Cols - 1; x >= 0; x--)
        {
            using var column = yellowMask.Col(x);
            if (Cv2.CountNonZero(column) <= 0)
            {
                continue;
            }

            right = x;
            break;
        }

        var top = -1;
        var bottom = -1;

        for (var y = 0; y < yellowMask.Rows; y++)
        {
            using var row = yellowMask.Row(y);
            if (Cv2.CountNonZero(row) <= 0)
            {
                continue;
            }

            top = y;
            break;
        }

        for (var y = yellowMask.Rows - 1; y >= 0; y--)
        {
            using var row = yellowMask.Row(y);
            if (Cv2.CountNonZero(row) <= 0)
            {
                continue;
            }

            bottom = y;
            break;
        }

        if (left < 0 || right <= left || top < 0 || bottom <= top)
        {
            return null;
        }

        var bounds = new Rect(left, top, right - left + 1, bottom - top + 1);
        if (bounds.Width < minimumWidth || occupiedColumns < minimumWidth || bounds.Height < MinimumBarHeightPixels)
        {
            return null;
        }

        var aspectRatio = bounds.Width / (double)Math.Max(1, bounds.Height);
        return aspectRatio >= MinimumBarAspectRatio ? bounds : null;
    }

    private static int FindMarkerX(Mat markerBinary, Rect fightBar)
    {
        var searchHeight = Math.Max(fightBar.Height * MarkerSearchHeightScale, MinimumMarkerSearchHeightPixels);
        var searchY = Math.Max(0, fightBar.Y - ((searchHeight - fightBar.Height) / 2));
        var searchBottom = Math.Min(markerBinary.Rows, searchY + searchHeight);
        var normalizedHeight = Math.Max(1, searchBottom - searchY);
        var searchRect = new Rect(fightBar.X, searchY, fightBar.Width, normalizedHeight);

        var markerX = -1;
        var bestColumnPixels = 0;

        for (var x = searchRect.Left; x < searchRect.Right; x++)
        {
            using var column = new Mat(markerBinary, new Rect(x, searchRect.Y, 1, searchRect.Height));
            var brightPixels = Cv2.CountNonZero(column);
            if (brightPixels <= bestColumnPixels)
            {
                continue;
            }

            bestColumnPixels = brightPixels;
            markerX = x;
        }

        return bestColumnPixels >= MinimumMarkerColumnPixels ? markerX : -1;
    }
}
