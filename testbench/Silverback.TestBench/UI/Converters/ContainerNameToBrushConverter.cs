// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace Silverback.TestBench.UI.Converters;

public class ContainerNameToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush[] BrushesSet =
    [
        Brushes.DarkBlue,
        Brushes.DarkOrange,
        Brushes.DarkRed,
        Brushes.DarkGreen,
        Brushes.DarkViolet,
        Brushes.DarkCyan,
        Brushes.DarkSlateBlue,
        Brushes.DarkSlateGray,
        Brushes.DarkOliveGreen,
        Brushes.OrangeRed
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string containerName)
            return Brushes.Black;

        if (int.TryParse(containerName.Split('-').Last(), out int containerIndex))
            return BrushesSet[(containerIndex - 1) % BrushesSet.Length];

        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
