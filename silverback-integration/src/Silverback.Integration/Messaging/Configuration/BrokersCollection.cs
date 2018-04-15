using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// The collection of <see cref="IBroker"/> instances.
    /// </summary>
    internal class BrokersCollection : IEnumerable<IBroker>, IDisposable
    {
        private readonly List<IBroker> _brokers = new List<IBroker>();

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
        public TBroker Add<TBroker>(Action<TBroker> config)
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

            return broker;
        }

        /// <summary>
        /// Gets the <see cref="IBroker"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        public IBroker Get(string name = null)
            => string.IsNullOrEmpty(name)
                ? Default
                : _brokers.FirstOrDefault(b => b.Name == name);

        /// <summary>
        /// Gets the <see cref="IBroker" /> with the specified name.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        public TBroker Get<TBroker>(string name = null) where TBroker : IBroker
            => (TBroker)Get(name);

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

        #region IDisposable

        /// <summary>
        /// Disposes all brokers that implement <see cref="IDisposable"/>.
        /// </summary>
        public void Dispose()
            => _brokers?.ForEach(b => (b as IDisposable)?.Dispose());

        #endregion
    }
}
