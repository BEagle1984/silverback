// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Silverback.TestBench.ViewModel.Logs;

namespace Silverback.TestBench.UI.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogLevel logLevel)
            return Brushes.Black;

        return logLevel switch
        {
            LogLevel.Warning => Brushes.Orange,
            LogLevel.Error => Brushes.Red,
            LogLevel.Fatal => Brushes.DarkRed,
            _ => Brushes.Black
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
