// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Represent an event message that is exposed to other services through a message broker.
    /// </summary>
    /// <seealso cref="IEvent" />
    /// <seealso cref="IIntegrationMessage" />
    /// <seealso cref="IMessage" />
    public interface IIntegrationEvent : IEvent, IIntegrationMessage
    {
    }
}