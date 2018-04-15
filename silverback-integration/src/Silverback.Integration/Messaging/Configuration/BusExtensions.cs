using System;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains the extension methods to store and retrieve the objects stored in the <see cref="IBus"/>
    /// items collection.
    /// </summary>
    public static class BusExtensions
    {
        private const string KeyPrefix = "Silverback.Integration.Configuration.";

        #region Items

        /// <summary>
        /// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static BrokersCollection GetBrokers(this IBus bus)
            => (BrokersCollection)bus.Items.GetOrAdd(KeyPrefix + "Brokers", _ => new BrokersCollection());

        #endregion

        #region GetBroker / GetProducer / GetConsumer

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> associated with the <see cref="IBus"/>.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        internal static IBroker GetBroker(this IBus bus, string name = null)
            => GetBrokers(bus).Get(name);

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> associated with the <see cref="IBus"/>.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        internal static TBroker GetBroker<TBroker>(this IBus bus, string name = null) where TBroker : IBroker
            => (TBroker)GetBroker(bus, name);

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IProducer" /> instance to produce messages to the
        /// specified endpoint.
        /// </summary>
        /// <returns></returns>
        internal static IProducer GetProducer(this IBus bus, IEndpoint endpoint)
            => GetBroker(bus, endpoint.BrokerName).GetProducer(endpoint);

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IConsumer" /> instance to consume messages from
        /// the specified endpoint.
        /// </summary>
        /// <returns></returns>
        internal static IConsumer GetConsumer(this IBus bus, IEndpoint endpoint)
            => GetBroker(bus, endpoint.BrokerName).GetConsumer(endpoint);

        #endregion

        #region Connect

        /// <summary>
        /// Initializes the connection to all configured endpoints.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="inboundEndpoints">The inbound endpoints.</param>
        /// <param name="outboundEndpoints">The outbound endpoints.</param>
        public static void ConnectBrokers(this IBus bus, IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints)
        {
            foreach (var broker in GetBrokers(bus))
            {
                broker.Connect(
                    GetBrokerEndpoints(broker, inboundEndpoints),
                    GetBrokerEndpoints(broker, outboundEndpoints));
            }
        }

        /// <summary>
        /// Gets the endpoints that belong to the specified broker.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoints">The endpoints.</param>
        /// <returns></returns>
        private static IEndpoint[] GetBrokerEndpoints(IBroker broker, IEndpoint[] endpoints)
        {
            if (endpoints == null) return Array.Empty<IEndpoint>();

            var result = endpoints.Where(e => e.BrokerName == broker.Name);

            if (broker.IsDefault)
            {
                result = result
                    .Union(endpoints.Where(e => string.IsNullOrEmpty(e.BrokerName)))
                    .Distinct();
            }

            return result.ToArray();
        }

        #endregion

        ///// <summary>
        ///// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        ///// </summary>
        ///// <param name="bus">The bus.</param>
        ///// <returns></returns>
        //public static List<IEndpoint> GetInboundEndpoints(this IBus bus)
        //    => (List<IEndpoint>)bus.Items.GetOrAdd(KeyPrefix + "InboundEndpoints", _ => new List<IEndpoint>());

        ///// <summary>
        ///// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        ///// </summary>
        ///// <param name="bus">The bus.</param>
        ///// <returns></returns>
        //public static List<IEndpoint> GetOutboundEndpoints(this IBus bus)
        //    => (List<IEndpoint>) bus.Items.GetOrAdd(KeyPrefix + "InboundEndpoints", _ => new List<IEndpoint>());
    }
}