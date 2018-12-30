// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public abstract class KafkaEndpoint : IEndpoint
    {
        protected KafkaEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the topic name.
        /// </summary>
        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();
    }
}
