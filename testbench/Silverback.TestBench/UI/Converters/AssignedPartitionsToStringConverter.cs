// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Silverback.TestBench.UI.Converters;

public class AssignedPartitionsToStringConverter : IMultiValueConverter
{
    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [int assignedPartitionsCount, int partitionsCount]
            ? $"{assignedPartitionsCount}/{partitionsCount}"
            : null;

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
