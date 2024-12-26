// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Windows.Input;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Containers;

public class AutoScalingViewModel : ViewModelBase
{
    private bool _isEnabled;

    private TimeSpan _interval = TimeSpan.FromSeconds(30);

    private double _chance = 1;

    private int _minInstances = 2;

    private int _maxInstances = 5;

    public AutoScalingViewModel()
    {
        ToggleEnabledCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
    }

    public ICommand ToggleEnabledCommand { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value, nameof(IsEnabled));
    }

    public TimeSpan Interval
    {
        get => _interval;
        set => SetProperty(ref _interval, value, nameof(Interval));
    }

    public double Chance
    {
        get => _chance;
        set => SetProperty(ref _chance, value, nameof(Chance));
    }

    public int MinInstances
    {
        get => _minInstances;
        set => SetProperty(ref _minInstances, value, nameof(MinInstances));
    }

    public int MaxInstances
    {
        get => _maxInstances;
        set => SetProperty(ref _maxInstances, value, nameof(MaxInstances));
    }
}
