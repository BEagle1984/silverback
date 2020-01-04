﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    public static class HeadersExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        public static Confluent.Kafka.Headers ToConfluentHeaders(this IEnumerable<MessageHeader> headers)
        {
            var kafkaHeaders = new Confluent.Kafka.Headers();
            headers.ForEach(header => kafkaHeaders.Add(header.Key, Encoding.GetBytes(header.Value)));
            return kafkaHeaders;
        }

        public static IEnumerable<MessageHeader> ToSilverbackHeaders(this Confluent.Kafka.Headers kafkaHeaders) =>
            kafkaHeaders.Select(kafkaHeader =>
                    new MessageHeader(
                        kafkaHeader.Key,
                        Encoding.GetString(kafkaHeader.GetValueBytes())))
                .ToList();
    }
}