using FishingBot.Core.Contracts;
using OpenCvSharp;

namespace FishingBot.Core.V2.Vision;

public sealed record class CastBarSignalData(
    Rect CastBarBounds,
    Rect GreenWindowBounds,
    Rect WhiteMarkerBounds,
    int WhiteMarkerX,
    bool GreenWindowVisible,
    bool WhiteMarkerVisible,
    bool MarkerInsideGreenWindow);

public sealed class CastBarDetectorV2
{
    private const int SearchPaddingX = 0;
    private const int SearchPaddingTop = 8;
    private const int SearchPaddingBottom = 12;
    private const int DarkBarKernelWidth = 81;
    private const int DarkBarKernelHeight = 5;
    private const int GreenMaskKernelWidth = 9;
    private const int GreenMaskKernelHeight = 3;
    private const int MinCastBarWidth = 120;
    private const int MinCastBarHeight = 10;
    private const int MaxCastBarHeight = 28;
    private const double MinCastBarAspectRatio = 6.0;
    private const int MissingMarkerX = -1;
    private const double MaxNormalizedConfidence = 1.0;
    private const double WidthConfidenceWeight = 0.5;
    private const double BaseConfidence = 0.5;
    private const double PresentGeometryConfidence = 0.25;
    private const double GeometryConfidenceOffset = 0.25;

    private static readonly Scalar DarkUpperBound = new(28, 28, 28);
    private static readonly Scalar GreenLowerBound = new(35, 80, 80);
    private static readonly Scalar GreenUpperBound = new(95, 255, 255);
    private static readonly Scalar WhiteLowerBound = new(0, 0, 220);
    private static readonly Scalar WhiteUpperBound = new(180, 40, 255);

    public SignalResult<CastBarSignalData> Detect(Mat frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Empty() || !TryFindDarkBar(frame, out var castBarBounds))
        {
            return SignalResult<CastBarSignalData>.NotDetected();
        }

        var searchRegion = ExpandBounds(
            castBarBounds,
            frame.Size(),
            SearchPaddingX,
            SearchPaddingTop,
            SearchPaddingBottom);
        var greenWindow = FindLargestRect(frame, searchRegion, CreateGreenMask);
        var whiteMarker = FindLargestRect(frame, searchRegion, CreateWhiteMask);

        var greenWindowVisible = greenWindow.Width > 0 && greenWindow.Height > 0;
        var whiteMarkerVisible = whiteMarker.Width > 0 && whiteMarker.Height > 0;
        var whiteMarkerX = whiteMarkerVisible ? whiteMarker.X + (whiteMarker.Width / 2) : MissingMarkerX;
        var markerInsideGreenWindow = greenWindowVisible
            && whiteMarkerVisible
            && whiteMarkerX >= greenWindow.Left
            && whiteMarkerX < greenWindow.Right;

        var confidence = CalculateConfidence(frame.Cols, castBarBounds, greenWindowVisible, whiteMarkerVisible);
        var data = new CastBarSignalData(
            CastBarBounds: castBarBounds,
            GreenWindowBounds: greenWindow,
            WhiteMarkerBounds: whiteMarker,
            WhiteMarkerX: whiteMarkerX,
            GreenWindowVisible: greenWindowVisible,
            WhiteMarkerVisible: whiteMarkerVisible,
            MarkerInsideGreenWindow: markerInsideGreenWindow);

        return SignalResult<CastBarSignalData>.Detected(confidence, data);
    }

    private static bool TryFindDarkBar(Mat frame, out Rect castBarBounds)
    {
        using var darkMask = new Mat();
        Cv2.InRange(frame, Scalar.Black, DarkUpperBound, darkMask);
        using var closedDarkMask = new Mat();
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(DarkBarKernelWidth, DarkBarKernelHeight));
        Cv2.MorphologyEx(darkMask, closedDarkMask, MorphTypes.Close, kernel);

        castBarBounds = FindLargestRect(
            closedDarkMask,
            new Rect(0, 0, frame.Cols, frame.Rows),
            static mask => mask.Clone(),
            IsCastBarCandidate);

        return castBarBounds.Width > 0 && castBarBounds.Height > 0;
    }

    private static Rect FindLargestRect(
        Mat frame,
        Rect searchRegion,
        Func<Mat, Mat> maskFactory,
        Func<Rect, bool>? predicate = null)
    {
        using var roi = new Mat(frame, searchRegion);
        using var mask = maskFactory(roi);
        Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var bestRect = default(Rect);
        var bestArea = 0;

        foreach (var contour in contours)
        {
            var localRect = Cv2.BoundingRect(contour);
            if (localRect.Width <= 0 || localRect.Height <= 0)
            {
                continue;
            }

            var globalRect = Offset(localRect, searchRegion.X, searchRegion.Y);
            if (predicate is not null && !predicate(globalRect))
            {
                continue;
            }

            var area = globalRect.Width * globalRect.Height;
            if (area <= bestArea)
            {
                continue;
            }

            bestRect = globalRect;
            bestArea = area;
        }

        return bestRect;
    }

    private static Mat CreateGreenMask(Mat roi)
    {
        var mask = new Mat();
        using var hsv = new Mat();
        using var closedMask = new Mat();
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(GreenMaskKernelWidth, GreenMaskKernelHeight));

        Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);
        Cv2.InRange(hsv, GreenLowerBound, GreenUpperBound, mask);
        Cv2.MorphologyEx(mask, closedMask, MorphTypes.Close, kernel);
        mask.Dispose();
        return closedMask.Clone();
    }

    private static Mat CreateWhiteMask(Mat roi)
    {
        var mask = new Mat();
        using var hsv = new Mat();

        Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);
        Cv2.InRange(hsv, WhiteLowerBound, WhiteUpperBound, mask);
        return mask;
    }

    private static bool IsCastBarCandidate(Rect rect)
    {
        var aspectRatio = rect.Width / (double)Math.Max(1, rect.Height);
        return rect.Width >= MinCastBarWidth
            && rect.Height is >= MinCastBarHeight and <= MaxCastBarHeight
            && aspectRatio >= MinCastBarAspectRatio;
    }

    private static Rect ExpandBounds(Rect rect, Size frameSize, int paddingX, int paddingTop, int paddingBottom)
    {
        var left = Math.Max(0, rect.X - paddingX);
        var top = Math.Max(0, rect.Y - paddingTop);
        var right = Math.Min(frameSize.Width, rect.Right + paddingX);
        var bottom = Math.Min(frameSize.Height, rect.Bottom + paddingBottom);
        return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private static Rect Offset(Rect rect, int offsetX, int offsetY)
        => new(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);

    private static double CalculateConfidence(int frameWidth, Rect castBarBounds, bool greenWindowVisible, bool whiteMarkerVisible)
    {
        var widthConfidence = Math.Clamp(castBarBounds.Width / (double)Math.Max(1, frameWidth), 0.0, MaxNormalizedConfidence);
        var geometryConfidence = (greenWindowVisible ? PresentGeometryConfidence : 0.0)
            + (whiteMarkerVisible ? PresentGeometryConfidence : 0.0);
        return Math.Clamp(
            (widthConfidence * WidthConfidenceWeight) + BaseConfidence + geometryConfidence - GeometryConfidenceOffset,
            0.0,
            MaxNormalizedConfidence);
    }
}
