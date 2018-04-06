using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Holds the collection of <see cref="IBroker"/> for the current instance.
    /// </summary>
    public class BrokersConfig : IEnumerable<IBroker>
    {
        #region Singleton

        private static readonly Lazy<BrokersConfig> InstanceValue = new Lazy<BrokersConfig>(true);

        /// <summary>
        /// Gets the current instance of the BrokerConfig.
        /// </summary>
        public static BrokersConfig Instance => InstanceValue.Value;

        #endregion

        private readonly HashSet<IBroker> _brokers =
            new HashSet<IBroker>();

        /// <summary>
        /// Gets the default <see cref="IBroker"/>.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        public IBroker Default { get; private set; }

        /// <summary>
        /// Gets the configurations count.
        /// </summary>
        public int Count => _brokers.Count;

        /// <summary>
        /// Adds the an <see cref="IBroker"/> of the specified type TBroker.
        /// </summary>
        /// <param name="config">The method applying the configuration.</param>
        public BrokersConfig Add<TBroker>(Action<TBroker> config)
            where TBroker : IBroker, new()
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var broker = new TBroker();
            config(broker);
            broker.ValidateConfiguration();

            lock (_brokers)
            {
                if (_brokers.Any(b => b.Name == broker.Name))
                    throw new InvalidOperationException($"A configuration with name \"{broker.Name}\" already exists.");
                if (broker.IsDefault && _brokers.Any(b => b.IsDefault))
                    throw new InvalidOperationException("Only one configuration can be marked as default.");

                _brokers.Add(broker);

                if (broker.IsDefault || _brokers.Count == 1)
                    Default = broker;
            }

            return this;
        }

        /// <summary>
        /// Gets the <see cref="IBroker"/> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public IBroker Get(string name)
            => _brokers.FirstOrDefault(b => b.Name == name);

        /// <summary>
        /// Gets the <see cref="IBroker" /> with the specified name.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="name">The name of the broker.</param>
        /// <returns></returns>
        public TBroker Get<TBroker>(string name) where TBroker : IBroker
            => (TBroker)Get(name);

        /// <summary>
        /// Gets the default <see cref="IBroker" />.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <returns></returns>
        public TBroker GetDefault<TBroker>() where TBroker : IBroker
            => (TBroker)Default;

        /// <summary>
        /// Clears all configurations.
        /// </summary>
        public void Clear()
            => _brokers.Clear();

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<IBroker> GetEnumerator()
            => _brokers.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
            => _brokers.GetEnumerator();

        #endregion
    }
}
