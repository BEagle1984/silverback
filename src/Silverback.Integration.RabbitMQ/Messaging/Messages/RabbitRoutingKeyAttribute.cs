// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The value of the property decorated with this attribute will be used as routing key. The routing key
    ///     can be used by RabbitMQ to route the messages to the proper queue.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RabbitRoutingKeyAttribute : Attribute
    {
    }
}
