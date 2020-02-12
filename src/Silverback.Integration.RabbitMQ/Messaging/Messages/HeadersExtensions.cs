// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Silverback.Messaging.Messages
{
    public static class HeadersExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        public static IDictionary<string, object> ToRabbitHeaders(this IEnumerable<MessageHeader> headers) =>
            headers.ToDictionary(
                header => header.Key,
                header => (object) Encoding.GetBytes(header.Value));

        public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(
            this IDictionary<string, object> rabbitHeaders) =>
            rabbitHeaders.Select(rabbitHeader =>
                    new MessageHeader(
                        rabbitHeader.Key,
                        Encoding.GetString((byte[]) rabbitHeader.Value)))
                .ToList();
    }
}