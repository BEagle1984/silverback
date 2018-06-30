using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Extensions;
using Silverback.Messaging.Adapters;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains a set of extention methods useful to setup the <see cref="IBus"/>.
    /// </summary>
    public static class BusExtensions
    {
        private const string ItemsKeyPrefix = "Silverback.Integration.Configuration.";

        #region Brokers

        /// <summary>
        /// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static BrokersCollection GetBrokers(this IBus bus)
            => (BrokersCollection)bus.Items.GetOrAdd(ItemsKeyPrefix + "Brokers", _ => new BrokersCollection());

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

        #region Inbound Adapters

        /// <summary>
        /// Gets the item with of the specified .
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="adapter">The adapter.</param>
        internal static void AddInboundAdapterItem(this IBus bus, IInboundAdapter adapter)
        {
            if (!bus.Items.TryAdd($"{ItemsKeyPrefix}InboundAdapter.{adapter.GetType().Name}.{Guid.NewGuid():N}", adapter))
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region Connect

        /// <summary>
        /// Connects to the message brokers to start consuming and producing messages.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public static void ConnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Connect());


        /// <summary>
        /// Disconnects from the message brokers.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public static void DisconnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Disconnect());

        #endregion
    }
}