using OpenCvSharp;
using FishingBot.Core.Vision;

namespace FishingBot.Core.Contracts;

public interface IVisionPipeline
{
    VisionSnapshot Analyze(Mat startPromptRoi, Mat aimRoi, Mat tensionRoi, Mat fightRoi, Mat catchMenuRoi);
}
