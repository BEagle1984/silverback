// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging.Messages;

internal static class KafkaHeadersMappingExtensions
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    public static Confluent.Kafka.Headers ToConfluentHeaders(this IReadOnlyList<MessageHeader> headers)
    {
        Confluent.Kafka.Headers kafkaHeaders = [];

        for (int i = 0; i < headers.Count; i++)
        {
            MessageHeader header = headers[i];

            if (!header.Name.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix, StringComparison.Ordinal))
                kafkaHeaders.Add(header.Name, Encode(header.Value));
        }

        return kafkaHeaders;
    }

    // The additional capacity reserves room for the headers that the caller is about to add
    public static MessageHeaderCollection ToSilverbackHeaders(this Confluent.Kafka.Headers? kafkaHeaders, int additionalCapacity = 0)
    {
        if (kafkaHeaders == null)
            return new MessageHeaderCollection(additionalCapacity);

        MessageHeaderCollection headers = new(kafkaHeaders.Count + additionalCapacity);

        for (int i = 0; i < kafkaHeaders.Count; i++)
        {
            IHeader kafkaHeader = kafkaHeaders[i];
            headers.Add(new MessageHeader(kafkaHeader.Key, Decode(kafkaHeader.GetValueBytes())));
        }

        return headers;
    }

    private static byte[]? Encode(string? value) => value != null ? Encoding.GetBytes(value) : null;

    private static string? Decode(byte[]? value) => value != null ? Encoding.GetString(value) : null;
}
