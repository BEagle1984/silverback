// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Can be placed on a subscribed method to filter the messages to be processed according to the name of the consumer that consumed them.
///     This is useful when having multiple consumers subscribed to the same topic.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ConsumerNameFilterAttribute : MessageFilterAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerNameFilterAttribute" /> class.
    /// </summary>
    /// <param name="groupId">
    ///     The list of group id whose messages have to be processed.
    /// </param>
    public ConsumerNameFilterAttribute(params string[] groupId)
    {
        GroupId = groupId;
    }

    /// <summary>
    ///     Gets the names of the consumers whose messages have to be processed.
    /// </summary>
    public string[] GroupId { get; }

    /// <inheritdoc cref="MessageFilterAttribute.MustProcess" />
    public override bool MustProcess(object message) =>
        MessageIsFromAllowedGroups(message) || message is IMessageStreamProvider;

    private bool MessageIsFromAllowedGroups(object message) =>
        message is IInboundEnvelope { Consumer: { } consumer } && GroupId.Contains(consumer.Name);
}
