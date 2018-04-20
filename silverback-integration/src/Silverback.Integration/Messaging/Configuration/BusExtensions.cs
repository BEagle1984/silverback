using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Extensions;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains the extension methods to store and retrieve the objects stored in the <see cref="IBus"/>
    /// items collection.
    /// </summary>
    public static class BusExtensions
    {
        private const string KeyPrefix = "Silverback.Integration.Configuration.";

        #region Brokers

        /// <summary>
        /// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static BrokersCollection GetBrokers(this IBus bus)
            => (BrokersCollection)bus.Items.GetOrAdd(KeyPrefix + "Brokers", _ => new BrokersCollection());

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
        
        #endregion

        #region Connect

        /// <summary>
        /// Connects to the message brokers to start consuming and producing messages.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="inboundEndpoints">The inbound endpoints.</param>
        /// <param name="outboundEndpoints">The outbound endpoints.</param>
        public static void ConnectBrokers(this IBus bus, IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints)
            => GetBrokers(bus).ForEach(b => b.Connect());


        /// <summary>
        /// Disconnects from the message brokers.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="inboundEndpoints">The inbound endpoints.</param>
        /// <param name="outboundEndpoints">The outbound endpoints.</param>
        public static void DisconnectBrokers(this IBus bus, IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints)
            => GetBrokers(bus).ForEach(b => b.Disconnect());

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