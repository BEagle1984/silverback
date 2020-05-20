// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The properties decorated with this attribute will be used
    ///     to build a key that will determine the destination partition on Kafka.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [Obsolete("PartitioningKeyMemberAttribute is deprecated, use KafkaKeyMemberAttribute instead.")]
    public sealed class PartitioningKeyMemberAttribute : Attribute
    {
    }
}