// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Can be placed on a subscribed method to filter the messages to be processed according to the group
///     id that consumed them. This is used when having multiple consumer groups for the same topic running
///     in the same process.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class KafkaGroupIdFilterAttribute : MessageFilterAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaGroupIdFilterAttribute" /> class.
    /// </summary>
    /// <param name="groupId">
    ///     The list of group id whose messages have to be processed.
    /// </param>
    public KafkaGroupIdFilterAttribute(params string[] groupId)
    {
        GroupId = groupId;
    }

    /// <summary>
    ///     Gets the list of group id whose messages have to be processed.
    /// </summary>
    public string[] GroupId { get; }

    /// <inheritdoc cref="MessageFilterAttribute.MustProcess" />
    public override bool MustProcess(object message) =>
        MessageIsFromAllowedGroups(message) || message is IMessageStreamProvider;

    private bool MessageIsFromAllowedGroups(object message) =>
        message is IInboundEnvelope { Consumer: KafkaConsumer consumer } && GroupId.Contains(consumer.Configuration.GroupId);
}
