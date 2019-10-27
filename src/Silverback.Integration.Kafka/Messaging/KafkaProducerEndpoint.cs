// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaProducerEndpoint : KafkaEndpoint, IProducerEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        public KafkaProducerEndpoint(string name) : base(name)
        {
        }

        public KafkaProducerConfig Configuration { get; set; } = new KafkaProducerConfig();

        public ChunkSettings Chunk { get; set; } = new ChunkSettings
        {
            Size = int.MaxValue
        };

        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();

            Chunk?.Validate();
        }

        #region Equality

        public bool Equals(KafkaProducerEndpoint other) => 
            base.Equals(other) && Equals(Configuration, other?.Configuration) && Equals(Chunk, other?.Chunk);

        public override bool Equals(object obj) => 
            base.Equals(obj) &&obj is KafkaProducerEndpoint endpoint && Equals(endpoint);

        #endregion
    }

#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}