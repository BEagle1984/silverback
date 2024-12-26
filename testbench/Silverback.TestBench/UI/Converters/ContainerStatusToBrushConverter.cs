// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.UI.Converters;

public class ContainerStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ContainerStatus status)
            return Brushes.Black;

        return status switch
        {
            ContainerStatus.Starting => Brushes.Orange,
            ContainerStatus.Running => Brushes.Green,
            ContainerStatus.Stopping => Brushes.Orange,
            ContainerStatus.Stopped => Brushes.Red,
            _ => Brushes.Black
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
