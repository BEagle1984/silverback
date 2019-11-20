// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The properties decorated with this attribute will be used
    /// to build the message key that will used by Kafka
    /// (for partitioning, compacting, etc.).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class KafkaKeyMemberAttribute : Attribute
    {
    }
}