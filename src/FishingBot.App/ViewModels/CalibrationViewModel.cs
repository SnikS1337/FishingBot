using FishingBot.Core.Config;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

namespace FishingBot.App.ViewModels;

public sealed class CalibrationViewModel : INotifyPropertyChanged
{
    private double _startPromptX;
    private double _startPromptY;
    private double _startPromptW;
    private double _startPromptH;
    private double _tensionWidgetX;
    private double _tensionWidgetY;
    private double _tensionWidgetW;
    private double _tensionWidgetH;
    private double _fightBarX;
    private double _fightBarY;
    private double _fightBarW;
    private double _fightBarH;
    private double _catchMenuX;
    private double _catchMenuY;
    private double _catchMenuW;
    private double _catchMenuH;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double StartPromptX { get => _startPromptX; set => SetProperty(ref _startPromptX, value); }
    public double StartPromptY { get => _startPromptY; set => SetProperty(ref _startPromptY, value); }
    public double StartPromptW { get => _startPromptW; set => SetProperty(ref _startPromptW, value); }
    public double StartPromptH { get => _startPromptH; set => SetProperty(ref _startPromptH, value); }

    public double TensionWidgetX { get => _tensionWidgetX; set => SetProperty(ref _tensionWidgetX, value); }
    public double TensionWidgetY { get => _tensionWidgetY; set => SetProperty(ref _tensionWidgetY, value); }
    public double TensionWidgetW { get => _tensionWidgetW; set => SetProperty(ref _tensionWidgetW, value); }
    public double TensionWidgetH { get => _tensionWidgetH; set => SetProperty(ref _tensionWidgetH, value); }

    public double FightBarX { get => _fightBarX; set => SetProperty(ref _fightBarX, value); }
    public double FightBarY { get => _fightBarY; set => SetProperty(ref _fightBarY, value); }
    public double FightBarW { get => _fightBarW; set => SetProperty(ref _fightBarW, value); }
    public double FightBarH { get => _fightBarH; set => SetProperty(ref _fightBarH, value); }

    public double CatchMenuX { get => _catchMenuX; set => SetProperty(ref _catchMenuX, value); }
    public double CatchMenuY { get => _catchMenuY; set => SetProperty(ref _catchMenuY, value); }
    public double CatchMenuW { get => _catchMenuW; set => SetProperty(ref _catchMenuW, value); }
    public double CatchMenuH { get => _catchMenuH; set => SetProperty(ref _catchMenuH, value); }

    public void LoadFrom(BotConfig config)
    {
        StartPromptX = config.Regions.StartPrompt.X;
        StartPromptY = config.Regions.StartPrompt.Y;
        StartPromptW = config.Regions.StartPrompt.W;
        StartPromptH = config.Regions.StartPrompt.H;

        TensionWidgetX = config.Regions.TensionWidget.X;
        TensionWidgetY = config.Regions.TensionWidget.Y;
        TensionWidgetW = config.Regions.TensionWidget.W;
        TensionWidgetH = config.Regions.TensionWidget.H;

        FightBarX = config.Regions.FightBar.X;
        FightBarY = config.Regions.FightBar.Y;
        FightBarW = config.Regions.FightBar.W;
        FightBarH = config.Regions.FightBar.H;

        CatchMenuX = config.Regions.CatchMenu.X;
        CatchMenuY = config.Regions.CatchMenu.Y;
        CatchMenuW = config.Regions.CatchMenu.W;
        CatchMenuH = config.Regions.CatchMenu.H;
    }

    public void ApplyTo(BotConfig config)
    {
        config.Regions.StartPrompt = BuildRect(StartPromptX, StartPromptY, StartPromptW, StartPromptH);
        config.Regions.TensionWidget = BuildRect(TensionWidgetX, TensionWidgetY, TensionWidgetW, TensionWidgetH);
        config.Regions.FightBar = BuildRect(FightBarX, FightBarY, FightBarW, FightBarH);
        config.Regions.CatchMenu = BuildRect(CatchMenuX, CatchMenuY, CatchMenuW, CatchMenuH);
    }

    private static NormalizedRect BuildRect(double x, double y, double w, double h)
    {
        return new NormalizedRect(
            Clamp01(x),
            Clamp01(y),
            ClampSize(w),
            ClampSize(h));
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }

    private static double ClampSize(double value)
    {
        return Math.Clamp(value, 0.01, 1.0);
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
