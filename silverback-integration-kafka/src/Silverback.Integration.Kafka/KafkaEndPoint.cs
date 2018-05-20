using Silverback.Messaging;
using System;
using System.Collections.Generic;

namespace Silverback
{
    /// <inheritdoc cref="IEndpoint"/>
    public sealed class KafkaEndpoint : IEndpoint, IEquatable<KafkaEndpoint>
    {
        /// <summary>
        /// The hash code referer.
        /// </summary>
        private readonly string _hashCodeReferer;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaEndpoint"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configs">The configs.</param>
        /// <param name="pollTimeOut">The timeout.</param>
        /// <param name="commitOffset">The commit offset.</param>
        /// <param name="brokerName">Name of the broker.</param>
        private KafkaEndpoint(string name, Dictionary<string, object> configs, int pollTimeOut = 100, int commitOffset = 1, string brokerName = null)
        {
            if (configs == null || configs.Count == 0 || !configs.TryGetValue("bootstrap.servers", out var serverAddress))
                throw new Exception("The configuration must contain at least the bootstrap.server key.");

            Name = name;
            Configuration = configs;
            BrokerName = brokerName;
            TimeoutPollBlock = pollTimeOut;
            CommitOffsetEach = commitOffset;
            _hashCodeReferer = $"{Name}-{BrokerName}-{TimeoutPollBlock}-{CommitOffsetEach}-{Configuration.Count}-{serverAddress}";
        }

        /// <summary>
        /// Creates new Kafka endpoint.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configs">The configs.</param>
        /// <param name="pollTimeOut">The time out.</param>
        /// <param name="commitOffset">The commit offset.</param>
        /// <param name="brokerName">Name of the broker.</param>
        /// <returns></returns>
        public static KafkaEndpoint Create(string name, Dictionary<string, object> configs, int pollTimeOut = 100, int commitOffset = 1, string brokerName = null)
        {
            return new KafkaEndpoint(name, configs, pollTimeOut, commitOffset, brokerName);
        }

        #region public properties
        /// <inheritdoc/>
        public string Name { get; set; }
        
        /// <inheritdoc/>
        public string BrokerName { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Dictionary<string, object> Configuration { get; set; }

        //TODO: Should be set by extension method?
        /// <summary>
        /// Define the number of message processed before committing the offset to the server.
        /// The most reliable level is one but it reduces throughput.
        /// </summary>
        /// <value>
        /// The commit offset.
        /// </value>
        public int CommitOffsetEach { get; set; }

        //TODO: Should be set by extension method?
        /// <summary>
        /// The maximum time (in milliseconds -1 to block indefinitely) within which the poll remain blocked.
        /// </summary>
        /// <value>
        /// The poll time out.
        /// </value>
        public int TimeoutPollBlock { get; set; }

        #endregion

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
            return brokerNameComparison != 0 ? brokerNameComparison : string.Compare(Name, other.Name, StringComparison.Ordinal);
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

        /// <inheritdoc />
        public bool Equals(KafkaEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(BrokerName, other.BrokerName)
                && string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase)
                && CompareConfiguration(Configuration, other.Configuration);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this inst ance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((KafkaEndpoint)obj);
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
                return 23 + _hashCodeReferer.GetHashCode();
            }
        }

        /// <summary>
        /// Compares the configuration.
        /// </summary>
        /// <param name="dict1">The dict1.</param>
        /// <param name="dict2">The dict2.</param>
        /// <returns></returns>
        private static bool CompareConfiguration(
            Dictionary<string, object> dict1, IReadOnlyDictionary<string, object> dict2)
        {
            if (dict1 == null || dict2 == null) return false;
            if (dict1.Count != dict2.Count) return false;

            var valueComparer = EqualityComparer<object>.Default;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value2)) return false;
                if (value2 is Dictionary<string, object> val2 && kvp.Value is Dictionary<string, object> val1 &&
                    CompareConfiguration(val2, val1))
                    continue;
                if (!valueComparer.Equals(kvp.Value, value2)) return false;
            }
            return true;
        }

        #endregion
    }
}
