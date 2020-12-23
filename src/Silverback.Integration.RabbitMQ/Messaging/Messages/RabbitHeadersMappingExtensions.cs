// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Silverback.Messaging.Messages
{
    internal static class RabbitHeadersMappingExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        public static IDictionary<string, object?> ToRabbitHeaders(this IEnumerable<MessageHeader> headers) =>
            headers.ToDictionary(
                header => header.Name,
                header => (object?)Encode(header.Value));

        public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(
            this IDictionary<string, object> rabbitHeaders) =>
            rabbitHeaders.Select(
                    rabbitHeader =>
                        new MessageHeader(
                            rabbitHeader.Key,
                            Decode((byte[]?)rabbitHeader.Value)))
                .ToList();

        private static byte[]? Encode(string? value) => value != null
            ? Encoding.GetBytes(value)
            : null;

        private static string? Decode(byte[]? value) => value != null ? Encoding.GetString(value) : null;
    }
}
