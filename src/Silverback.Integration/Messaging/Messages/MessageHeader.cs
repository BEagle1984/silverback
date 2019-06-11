// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    public class MessageHeader
    {
        public const string FailedAttemptsHeaderName = "Silverback.FailedAttempts";

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