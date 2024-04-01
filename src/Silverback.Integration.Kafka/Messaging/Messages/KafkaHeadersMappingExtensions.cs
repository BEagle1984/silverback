// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Text;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal static class KafkaHeadersMappingExtensions
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    public static Confluent.Kafka.Headers ToConfluentHeaders(this IEnumerable<MessageHeader> headers)
    {
        Confluent.Kafka.Headers kafkaHeaders = [];
        headers.ForEach(
            header =>
            {
                if (header.Name == DefaultMessageHeaders.MessageId)
                    return;

                kafkaHeaders.Add(header.Name, Encode(header.Value));
            });
        return kafkaHeaders;
    }

    public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(this Confluent.Kafka.Headers? kafkaHeaders)
    {
        List<MessageHeader> headers = new(kafkaHeaders?.Count ?? 0);
        kafkaHeaders?.ForEach(kafkaHeader => headers.Add(new MessageHeader(kafkaHeader.Key, Decode(kafkaHeader.GetValueBytes()))));

        return headers;
    }

    private static byte[]? Encode(string? value) => value != null ? Encoding.GetBytes(value) : null;

    private static string? Decode(byte[]? value) => value != null ? Encoding.GetString(value) : null;
}
