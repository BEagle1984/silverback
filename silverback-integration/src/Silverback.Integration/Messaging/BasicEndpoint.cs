using System;
using Newtonsoft.Json;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging
{
    /// <summary>
    /// A simple <see cref="IEndpoint"/> containing the basic information.
    /// Can be used as base class for more complex endpoints.
    /// </summary>
    /// <seealso cref="IEndpoint" />
    public sealed class BasicEndpoint : IEndpoint, IEquatable<BasicEndpoint>
    {
        /// <summary>
        /// Gets or sets the name of the broker to be used.
        /// If not set the default one will be used.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BrokerName { get; }

        /// <summary>
        /// Gets or sets the topic/queue name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Prevents a default instance of the <see cref="BasicEndpoint" /> class from being created.
        /// The Create method is supposed to be used to create the object.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="brokerName">The name of the broker.</param>
        [JsonConstructor]
        private BasicEndpoint(string name, string brokerName = null)
        {
            Name = name;
            BrokerName = brokerName;
        }

        /// <summary>
        /// Creates a new <see cref="BasicEndpoint" /> pointing to the specified topic/queue.
        /// </summary>
        /// <param name="name">The queue/topic name.</param>
        /// <param name="brokerName">The name of the broker.</param>
        /// <returns></returns>
        public static BasicEndpoint Create(string name, string brokerName = null)
            => new BasicEndpoint(name, brokerName);

        #region IComparable

        /// <summary>
        /// Compares this instance to another <see cref="BasicEndpoint"/>.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns></returns>
        public int CompareTo(BasicEndpoint other)
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
            if (!(obj is BasicEndpoint)) throw new ArgumentException($"Object must be of type {nameof(BasicEndpoint)}");
            return CompareTo((BasicEndpoint)obj);
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
        public bool Equals(BasicEndpoint other)
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
            return Equals((BasicEndpoint)obj);
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
                return ((BrokerName != null ? BrokerName.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        #endregion

    }
}