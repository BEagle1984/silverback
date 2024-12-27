// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using Silverback.TestBench.ViewModel.Trace;

namespace Silverback.TestBench.UI.Converters;

public class MessageTraceStatusFilterDecoderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MessageTraceStatusFilter statusFilter)
            return null;

        return statusFilter switch
        {
            MessageTraceStatusFilter.Pending => "Pending",
            MessageTraceStatusFilter.NotConsumed => "Not consumed",
            MessageTraceStatusFilter.Error => "Error",
            MessageTraceStatusFilter.Processed => "Processed",
            MessageTraceStatusFilter.Any => "Any",
            _ => throw new ArgumentOutOfRangeException(nameof(parameter))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
