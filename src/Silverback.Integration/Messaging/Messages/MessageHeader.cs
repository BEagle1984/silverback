// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.Design;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    public class MessageHeader
    {
        private string _key;
        private string? _value;

        public MessageHeader()
        {
        }

        public MessageHeader(string key, object? value)
            : this(key, value?.ToString())
        {
        }

        public MessageHeader(string key, string? value)
        {
            Key = Check.NotNull(key, nameof(key));
            Value = value ?? string.Empty;
        }

        public string Key
        {
            get => _key;
            set => _key = Check.NotNull(value, nameof(Key));
        }

        public string? Value
        {
            get => _value;
            set => _value = value;
        }
    }
}