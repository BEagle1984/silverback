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
    public class BasicEndpoint : IEndpoint
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

        #region Get Broker / Producer / Consumer

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> to be used.
        /// </summary>
        /// <returns></returns>
        public IBroker GetBroker()
            => string.IsNullOrEmpty(BrokerName)
                ? BrokersConfig.Instance.Default
                : BrokersConfig.Instance.Get(BrokerName);

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> to be used casting it to the specified type.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <returns></returns>
        public TBroker GetBroker<TBroker>() where TBroker : IBroker
            => (TBroker)GetBroker();

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IProducer" /> instance to produce messages to this endpoint.
        /// </summary>
        /// <returns></returns>
        public virtual IProducer GetProducer()
            => GetBroker().GetProducer(this);

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IConsumer" /> instance to consume messages from this enpoint.
        /// </summary>
        /// <returns></returns>
        public virtual IConsumer GetConsumer()
            => GetBroker().GetConsumer(this);

        #endregion
    }
}