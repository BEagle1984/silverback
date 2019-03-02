// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaProducerConfig : Confluent.Kafka.ProducerConfig, ICollection<KeyValuePair<string, string>>, IEquatable<KafkaProducerConfig>
    {
        public void Validate()
        {
        }

        #region IEquatable

        public bool Equals(KafkaProducerConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return KafkaClientConfigComparer.Compare(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaProducerConfig)obj);
        }

        #endregion

        #region ICollection

        public void Add(KeyValuePair<string, string> item) => SetObject(item.Key, item.Value);

        public void Clear() => properties.Clear();

        public bool Contains(KeyValuePair<string, string> item) => properties.ToList().Contains(item);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
            properties.ToList().CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string> item) => properties.Remove(item.Key);

        public int Count => properties.Count;

        public bool IsReadOnly => false;

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}