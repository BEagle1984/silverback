// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Represent a message that is exposed to other services through a message broker.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Messages.IMessage" />
    public interface IIntegrationMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the message unique identifier.
        /// </summary>
        Guid Id { get; set; }

        // TODO: Need more properties like SourceTopic etc.?
    }
}
