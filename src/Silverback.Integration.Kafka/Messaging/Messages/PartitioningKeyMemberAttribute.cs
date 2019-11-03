// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The <see cref="KafkaBroker"/> will use the properties decorated with this attribute
    /// to build a key that will be used to determine the destination partition on Kafka.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PartitioningKeyMemberAttribute : Attribute
    {
    }
}