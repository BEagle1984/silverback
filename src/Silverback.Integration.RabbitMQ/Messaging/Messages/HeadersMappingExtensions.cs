// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Silverback.Messaging.Messages
{
    internal static class HeadersMappingExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static IDictionary<string, object?> ToRabbitHeaders(this IEnumerable<MessageHeader> headers) =>
            headers.ToDictionary(
                header => header.Name,
                header => (object?)Encode(header.Value));

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(
            this IDictionary<string, object> rabbitHeaders) =>
            rabbitHeaders.Select(
                    rabbitHeader =>
                        new MessageHeader(
                            rabbitHeader.Key,
                            Decode((byte[]?)rabbitHeader.Value)))
                .ToList();

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private static byte[]? Encode(string? value) => value != null
            ? Encoding.GetBytes(value)
            : null;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private static string? Decode(byte[]? value) => value != null ? Encoding.GetString(value) : null;
    }
}
