// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Silverback.TestBench.UI.Converters;

public class ContainerNameShortenerConverter : IValueConverter
{
    private const string NamePrefix = "silverback-testbench-";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string containerName && containerName.StartsWith(NamePrefix, StringComparison.Ordinal)
            ? containerName[NamePrefix.Length..]
            : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
