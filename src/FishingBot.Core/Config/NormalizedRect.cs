using System.Drawing;

namespace FishingBot.Core.Config;

public readonly record struct NormalizedRect(double X, double Y, double W, double H)
{
    public Rectangle ToPixelRect(int screenWidth, int screenHeight)
    {
        return new Rectangle(
            x: (int)Math.Round(X * screenWidth),
            y: (int)Math.Round(Y * screenHeight),
            width: (int)Math.Round(W * screenWidth),
            height: (int)Math.Round(H * screenHeight));
    }
}
