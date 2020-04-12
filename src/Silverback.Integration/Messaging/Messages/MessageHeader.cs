// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    public class MessageHeader
    {
        private string _key;
        private string _value;

        public MessageHeader()
        {
        }

        public MessageHeader(string key, object value)
            : this(key, value?.ToString())
        {
        }

        public MessageHeader(string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
        }

        public string Key
        {
            get => _key;
            set => _key = value ?? throw new ArgumentNullException(nameof(Key));
        }

        public string Value
        {
            get => _value;
            set => _value = value;
        }
    }
}