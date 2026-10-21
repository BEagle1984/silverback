// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Util;
using DekafHeader = Dekaf.Serialization.Header;
using DekafHeaders = Dekaf.Serialization.Headers;

namespace Silverback.Messaging.Messages;

internal static class KafkaHeadersMappingExtensions
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    public static DekafHeaders ToDekafHeaders(this IEnumerable<MessageHeader> headers)
    {
        DekafHeaders kafkaHeaders = [];
        headers
            .Where(header => !header.Name.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix, StringComparison.Ordinal))
            .ForEach(header => kafkaHeaders.Add(header.Name, Encode(header.Value)));
        return kafkaHeaders;
    }

    // The Dekaf header values point into pooled fetch buffers, therefore they are decoded right away
    public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(this IReadOnlyList<DekafHeader>? kafkaHeaders)
    {
        if (kafkaHeaders == null || kafkaHeaders.Count == 0)
            return [];

        List<MessageHeader> headers = new(kafkaHeaders.Count);

        foreach (DekafHeader kafkaHeader in kafkaHeaders)
        {
            headers.Add(new MessageHeader(kafkaHeader.Key, kafkaHeader.IsValueNull ? null : Encoding.GetString(kafkaHeader.Value.Span)));
        }

        return headers;
    }

    private static byte[]? Encode(string? value) => value != null ? Encoding.GetBytes(value) : null;
}
