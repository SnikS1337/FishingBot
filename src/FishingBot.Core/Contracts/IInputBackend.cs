using System.Drawing;

namespace FishingBot.Core.Contracts;

public sealed record InputBinding
{
    public InputBinding(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Input binding token cannot be blank.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; } = string.Empty;
}

public interface IInputBackend
{
    void TapKey(InputBinding key);

    void KeyDown(InputBinding key);

    void KeyUp(InputBinding key);

    void MoveMouse(Point point);

    void Click(Point point);

    void ReleaseAll();
}
