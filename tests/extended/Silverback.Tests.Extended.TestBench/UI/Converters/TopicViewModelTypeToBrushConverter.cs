// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Silverback.Tests.Extended.TestBench.ViewModel.Topics;

namespace Silverback.Tests.Extended.TestBench.UI.Converters;

public class TopicViewModelTypeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            KafkaTopicViewModel => Brushes.Purple,
            MqttTopicViewModel => Brushes.DarkBlue,
            _ => Brushes.Black
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
