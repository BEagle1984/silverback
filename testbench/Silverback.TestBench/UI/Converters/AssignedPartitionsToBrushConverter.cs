// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Silverback.TestBench.UI.Converters;

public class AssignedPartitionsToBrushConverter : IMultiValueConverter
{
    public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [int assignedPartitionsCount, int partitionsCount])
            return Brushes.Black;

        if (assignedPartitionsCount == partitionsCount)
            return Brushes.Green;

        if (assignedPartitionsCount == 0)
            return Brushes.DarkMagenta;

        if (assignedPartitionsCount < partitionsCount)
            return Brushes.Red;

        return Brushes.Orange;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
