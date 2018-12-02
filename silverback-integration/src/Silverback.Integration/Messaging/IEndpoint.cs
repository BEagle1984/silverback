// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public interface IEndpoint
    {
        /// <summary>
        /// Gets the topic/queue name.
        /// </summary>
        string Name { get; }

        IMessageSerializer Serializer { get; }
    }
}