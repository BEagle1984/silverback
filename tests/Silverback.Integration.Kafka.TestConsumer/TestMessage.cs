// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Silverback.Integration.Kafka.Messages
{
    public class TestMessage : IMessage
    {
        public Guid Id { get; set; }

        public string Text { get; set; }
    }
}