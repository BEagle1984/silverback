// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
    public class KafkaProducerEndpoint : KafkaEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        public KafkaProducerEndpoint(string name) : base(name)
        {
        }

        public Confluent.Kafka.ProducerConfig Configuration { get; set; } = new Confluent.Kafka.ProducerConfig();

        #region IEquatable

        public bool Equals(KafkaProducerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Serializer, other.Serializer) && KafkaClientConfigComparer.Compare(Configuration, other.Configuration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is KafkaProducerEndpoint && Equals((KafkaProducerEndpoint)obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        #endregion
    }
}