// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Silverback.TestBench.UI.Converters;

public class StringShortenerConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int maxLength = parameter is int intParam ? intParam : 50;

        if (value is string stringValue)
        {
            stringValue = stringValue.Trim();

            if (stringValue.Length > maxLength)
                return string.Concat(stringValue.AsSpan(0, maxLength), "...");
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
