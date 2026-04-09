using System.Windows.Threading;
using System;

namespace FishingBot.App.Services;

public sealed class UiDispatcher
{
    private readonly Dispatcher _dispatcher;

    public UiDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Post(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _ = _dispatcher.BeginInvoke(action);
    }
}
