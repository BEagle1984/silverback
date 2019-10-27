// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    public class MessageHeader
    {
        public const string MessageIdKey = "x-message-id";
        public const string MessageTypeKey = "x-message-type";
        public const string FailedAttemptsKey = "x-failed-attempts";
        public const string ChunkIdKey = "x-chunk-id";
        public const string ChunksCountKey = "x-chunks-count";
        public const string BatchIdKey = "x-batch-id";
        public const string BatchSizeKey = "x-batch-size";

        public MessageHeader()
        {
        }

        public MessageHeader(string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}