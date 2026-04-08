using System.Drawing;

namespace FishingBot.Core.Contracts;

public interface IInputEngine
{
    void PressE();

    void PressSpace();

    void HoldA();

    void HoldD();

    void ReleaseAD();

    void ReleaseAll();

    void ClickAt(Point target);
}
