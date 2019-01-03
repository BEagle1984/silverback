// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class KafkaProducerEndpoint : KafkaEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        public KafkaProducerEndpoint(string name) : base(name)
        {
        }

        public Confluent.Kafka.ProducerConfig Configuration { get; set; } = new Confluent.Kafka.ProducerConfig();

        #region IEquatable

        public bool Equals(KafkaProducerEndpoint other)
        {
            return base.Equals(other) && KafkaClientConfigComparer.Compare(Configuration, other.Configuration);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj) &&
                   obj is KafkaProducerEndpoint endpoint && Equals(endpoint);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}