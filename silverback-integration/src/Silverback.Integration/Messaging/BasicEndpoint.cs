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
    /// TODO: Test IComparable
    public class BasicEndpoint : IEndpoint, IComparable<BasicEndpoint>, IComparable
    {
        #region Construction

        private BasicEndpoint()
        {
        }

        /// <summary>
        /// Creates a new <see cref="BasicEndpoint"/> pointing to the specified topic/queue.
        /// </summary>
        /// <param name="name">The queue/topic name.</param>
        /// <returns></returns>
        public static BasicEndpoint Create(string name)
            => new BasicEndpoint { Name = name };

            #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the broker to be used.
        /// If not set the default one will be used.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BrokerName { get; set; }

        /// <summary>
        /// Gets or sets the topic/queue name.
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region Configuration

        /// <summary>
        /// Specifies which broker configuration is to be used.
        /// </summary>
        /// <param name="brokerName">The name of the broker.</param>
        /// <returns></returns>
        public BasicEndpoint UseBroker(string brokerName)
        {
            BrokerName = brokerName;
            return this;
        }

        /// <summary>
        /// Called after the fluent configuration is applied, should verify the consistency of the
        /// configuration.
        /// </summary>
        /// <remarks>
        /// An exception must be thrown if the confgiuration is not conistent.
        /// </remarks>
        public virtual void ValidateConfiguration()
        {
        }

        #endregion

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
            return CompareTo((BasicEndpoint) obj);
        }

        #endregion
    }
}