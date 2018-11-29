// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public sealed class KafkaEndpoint : IEndpoint, IEquatable<KafkaEndpoint>
    {
        public KafkaEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the topic name.
        /// </summary>
        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

        public KafkaConfigurationDictionary Configuration { get; set; }

        /// <summary>
        /// Define the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; } = 1;

        /// <summary>
        /// The maximum time (in milliseconds, -1 to block indefinitely) within which the poll remain blocked.
        /// </summary>
        public int PollTimeout { get; } = 100;

        #region IEquatable

        public bool Equals(KafkaEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Serializer, other.Serializer) && Equals(Configuration, other.Configuration) && CommitOffsetEach == other.CommitOffsetEach && PollTimeout == other.PollTimeout;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is KafkaEndpoint && Equals((KafkaEndpoint)obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        #endregion
    }
}
