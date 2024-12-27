// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Silverback.TestBench.ViewModel.Trace;

namespace Silverback.TestBench.UI.Converters;

public class MessageTraceStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MessageTraceStatus status)
            return Brushes.Black;

        return status switch
        {
            MessageTraceStatus.Produced => Brushes.DarkBlue,
            MessageTraceStatus.Processing => Brushes.DarkOrange,
            MessageTraceStatus.ProcessingError => Brushes.Red,
            MessageTraceStatus.Lost => Brushes.DarkRed,
            MessageTraceStatus.Processed => Brushes.DarkGreen,
            _ => Brushes.Black
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
