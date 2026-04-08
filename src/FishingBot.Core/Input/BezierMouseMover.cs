using System.Drawing;

namespace FishingBot.Core.Input;

public sealed class BezierMouseMover
{
    private readonly Random _random;

    public BezierMouseMover(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public void MoveMouse(Point from, Point to, int steps = 20)
    {
        var cp1 = new Point(
            from.X + _random.Next(-50, 51),
            from.Y + _random.Next(-50, 51));

        var cp2 = new Point(
            to.X + _random.Next(-50, 51),
            to.Y + _random.Next(-50, 51));

        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var x = Cubic(from.X, cp1.X, cp2.X, to.X, t);
            var y = Cubic(from.Y, cp1.Y, cp2.Y, to.Y, t);

            SendInputNative.SetCursorPos((int)x, (int)y);
            Thread.Sleep(_random.Next(8, 18));
        }
    }

    private static double Cubic(double p0, double p1, double p2, double p3, double t)
    {
        var oneMinusT = 1.0 - t;
        return Math.Pow(oneMinusT, 3) * p0
             + 3 * Math.Pow(oneMinusT, 2) * t * p1
             + 3 * oneMinusT * t * t * p2
             + t * t * t * p3;
    }
}
