using Silverback.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback
{
    public sealed class KafkaEndpoint : IEndpoint, IEquatable<KafkaEndpoint>
    {
        public KafkaEndpoint(string name, Dictionary<string, object> configs, string brokerName = null)
        {
            Name = name;
            Configuration = configs;
            BrokerName = brokerName;
        }

        public static KafkaEndpoint Create(string name, Dictionary<string, object> configs)
        {
            return new KafkaEndpoint(name, configs);
        }
        

        public string Name { get; set; }

        public string BrokerName { get; set; }

        public Dictionary<string, object> Configuration { get; set; }

        #region IComparable

        /// <summary>
        /// Compares this instance to another <see cref="KafkaEndpoint"/>.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns></returns>
        public int CompareTo(KafkaEndpoint other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var brokerNameComparison = string.Compare(BrokerName, other.BrokerName, StringComparison.Ordinal);
            if (brokerNameComparison != 0) return brokerNameComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this instance to another <see cref="object"/>.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">BasicEndpoint</exception>
        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is KafkaEndpoint)) throw new ArgumentException($"Object must be of type {nameof(KafkaEndpoint)}");
            return CompareTo((KafkaEndpoint)obj);
        }

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
        /// </returns>
        public bool Equals(KafkaEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(BrokerName, other.BrokerName) && string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KafkaEndpoint)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {                
                int hash = 17;
                hash = hash * 23 + (BrokerName ?? "").GetHashCode();
                hash = hash * 23 + (Name ?? "").GetHashCode();
                hash = hash * 23 + (Configuration != null ? Configuration.GetHashCode() : 0);
                return hash;
            }
        }

        #endregion
    }
}
