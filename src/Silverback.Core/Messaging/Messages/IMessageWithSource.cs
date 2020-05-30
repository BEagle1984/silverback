// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Exposes a <c>Source</c> property referencing the object that generated the message.
    /// </summary>
    public interface IMessageWithSource
    {
        /// <summary>
        ///     Gets or sets the reference to the object that generated the message.
        /// </summary>
        object? Source { get; set; }
    }
}
