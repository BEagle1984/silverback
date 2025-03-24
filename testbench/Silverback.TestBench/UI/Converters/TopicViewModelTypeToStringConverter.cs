// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Windows.Data;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.UI.Converters;

public class TopicViewModelTypeToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            KafkaTopicViewModel => "Kafka",
            MqttTopicViewModel => "MQTT",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
