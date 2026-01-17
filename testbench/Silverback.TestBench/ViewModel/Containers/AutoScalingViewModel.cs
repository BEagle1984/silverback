// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Windows.Input;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Containers;

public class AutoScalingViewModel : ViewModelBase
{
    public AutoScalingViewModel()
    {
        ToggleEnabledCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
    }

    public ICommand ToggleEnabledCommand { get; }

    public bool IsEnabled
    {
        get;
        set => SetProperty(ref field, value, nameof(IsEnabled));
    }

    public TimeSpan Interval
    {
        get;
        set => SetProperty(ref field, value, nameof(Interval));
    } = TimeSpan.FromSeconds(30);

    public double Chance
    {
        get;
        set => SetProperty(ref field, value, nameof(Chance));
    } = 1;

    public int MinInstances
    {
        get;
        set => SetProperty(ref field, value, nameof(MinInstances));
    } = 2;

    public int MaxInstances
    {
        get;
        set => SetProperty(ref field, value, nameof(MaxInstances));
    } = 5;
}
