// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Messages
{
    public class TestMessage : IMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }
    }
}
