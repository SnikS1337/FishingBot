using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class CatchMenuDetector
{
    private readonly Mat? _takeTemplate;
    private readonly Mat? _releaseTemplate;

    public CatchMenuDetector(string? takeTemplatePath = null, string? releaseTemplatePath = null)
    {
        if (!string.IsNullOrWhiteSpace(takeTemplatePath) && File.Exists(takeTemplatePath))
        {
            _takeTemplate = Cv2.ImRead(takeTemplatePath, ImreadModes.Color);
        }

        if (!string.IsNullOrWhiteSpace(releaseTemplatePath) && File.Exists(releaseTemplatePath))
        {
            _releaseTemplate = Cv2.ImRead(releaseTemplatePath, ImreadModes.Color);
        }
    }

    public DetectionResult Detect(Mat frame)
    {
        if (frame.Empty())
        {
            return new DetectionResult(false, 0);
        }

        using var gray = new Mat();
        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        using var darkMask = new Mat();
        Cv2.Threshold(gray, darkMask, 45, 255, ThresholdTypes.BinaryInv);

        var darkRatio = Cv2.CountNonZero(darkMask) / (double)(frame.Rows * frame.Cols);
        var isMenuLikely = darkRatio > 0.20;

        var confidence = Math.Min(1.0, darkRatio * 2.0);

        Point? take = null;
        Point? release = null;

        if (_takeTemplate is not null)
        {
            take = FindTemplateCenter(frame, _takeTemplate, 0.72);
        }

        if (_releaseTemplate is not null)
        {
            release = FindTemplateCenter(frame, _releaseTemplate, 0.72);
        }

        if (take is not null || release is not null)
        {
            isMenuLikely = true;
            confidence = Math.Max(confidence, 0.85);
        }

        return new DetectionResult(isMenuLikely, confidence, -1, take, release);
    }

    private static Point? FindTemplateCenter(Mat frame, Mat template, double threshold)
    {
        if (frame.Rows < template.Rows || frame.Cols < template.Cols)
        {
            return null;
        }

        using var result = new Mat();
        Cv2.MatchTemplate(frame, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

        if (maxVal < threshold)
        {
            return null;
        }

        return new Point(maxLoc.X + template.Cols / 2, maxLoc.Y + template.Rows / 2);
    }
}
