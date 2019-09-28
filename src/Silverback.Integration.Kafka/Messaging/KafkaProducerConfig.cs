// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Proxies;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaProducerConfig : ConfluentProducerConfigProxy, IEquatable<KafkaProducerConfig>
    {
        public void Validate()
        {
        }

        #region IEquatable

        public bool Equals(KafkaProducerConfig other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return KafkaClientConfigComparer.Compare(this.ConfluentConfig, other.ConfluentConfig);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaProducerConfig)obj);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}