// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal static class HeadersMappingExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        public static Confluent.Kafka.Headers ToConfluentHeaders(this IEnumerable<MessageHeader> headers)
        {
            var kafkaHeaders = new Confluent.Kafka.Headers();
            headers.ForEach(header => kafkaHeaders.Add(header.Name, Encode(header.Value)));
            return kafkaHeaders;
        }

        public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(
            this Confluent.Kafka.Headers kafkaHeaders) =>
            kafkaHeaders.Select(
                    kafkaHeader =>
                        new MessageHeader(
                            kafkaHeader.Key,
                            Decode(kafkaHeader.GetValueBytes())))
                .ToList();

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private static byte[]? Encode(string? value) => value != null
            ? Encoding.GetBytes(value)
            : null;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private static string? Decode(byte[]? value) => value != null ? Encoding.GetString(value) : null;
    }
}
