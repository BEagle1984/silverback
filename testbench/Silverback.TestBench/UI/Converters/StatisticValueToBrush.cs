// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Silverback.TestBench.UI.Converters;

public class StatisticValueToBrush : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int statisticValue)
            return Brushes.Black;

        int threshold = parameter == null ? 0 : int.Parse((string)parameter, CultureInfo.InvariantCulture);

        if (statisticValue > threshold)
            return Brushes.Red;

        if (statisticValue > threshold / 2)
            return Brushes.Orange;

        return Brushes.Green;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
