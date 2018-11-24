using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging
{
    /// <summary>
    /// Extends the Kafka configuration dictionary.
    /// </summary>
    /// <seealso cref="Dictionary{TKey, TValue}" />
    public class KafkaConfigurationDictionary : Dictionary<string, object>, IEquatable<KafkaConfigurationDictionary>
    {
        public bool IsAutocommitEnabled
        {
            get => ContainsKey("enable.auto.commit") && (bool)this["enable.auto.commit"];
            set => this["enable.auto.commit"] = value;
        }

        public void Validate()
        {
            if (!ContainsKey("bootstrap.servers"))
                throw new SilverbackException("The configuration must contain at least the 'bootstrap.server' key.");
        }

        public bool Equals(KafkaConfigurationDictionary other)
        {
            if (other == null)
                return false;

            if (other == this)
                return true;

            if (other.Keys.Count != Keys.Count)
                return false;

            return !this.Except(other).Any();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaConfigurationDictionary) obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}