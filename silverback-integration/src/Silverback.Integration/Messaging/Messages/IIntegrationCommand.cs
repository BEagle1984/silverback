// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Represent a command message that is sent to another service through a message broker.
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IIntegrationCommand : ICommand, IIntegrationMessage
    {
    }
}