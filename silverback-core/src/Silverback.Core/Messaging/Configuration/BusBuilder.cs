using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Builds the bus instance.
    /// </summary>
    /// <typeparam name="TBus">The type of the bus.</typeparam>
    public class BusBuilder<TBus>
        where TBus : IBus, new()
    {
        private readonly TBus _bus = new TBus();

        #region Build

        /// <summary>
        /// Builds and returns the bus instance.
        /// </summary>
        /// <returns></returns>
        public TBus Build()
        {
            if (_bus.GetTypeFactory() == null)
                WithDefaultFactory();

            if (_bus.GetLoggerFactory() == null)
                _bus.SetLoggerFactory(NullLoggerFactory.Instance);

            return _bus;
        }

        #endregion

        #region WithFactory

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory method used to instanciate a single instance.</param>
        /// <returns></returns>
        public BusBuilder<TBus> WithFactory(Func<Type, object> singleInstanceFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(singleInstanceFactory));
            return this;
        }

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="multiInstancesFactory">TThe factory method used to instanciate multiple instances of a type.</param>
        /// <returns></returns>
        public BusBuilder<TBus> WithFactory(Func<Type, object[]> multiInstancesFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(multiInstancesFactory));
            return this;
        }

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory method used to instanciate a single instance.</param>
        /// <param name="multiInstancesFactory">TThe factory method used to instanciate multiple instances of a type.</param>
        /// <returns></returns>
        public BusBuilder<TBus> WithFactory(Func<Type, object> singleInstanceFactory, Func<Type, object[]> multiInstancesFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(singleInstanceFactory, multiInstancesFactory));
            return this;
        }

        /// <summary>
        /// Set the <see cref="ITypeFactory" /> to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public BusBuilder<TBus> WithFactory(ITypeFactory factory)
        {
            _bus.SetTypeFactory(factory);
            return this;
        }

        /// <summary>
        /// Setup a factory that uses reflection to instanciate the subscribers.
        /// All types must have a parameterless constructor.
        /// </summary>
        /// <returns></returns>
        public BusBuilder<TBus> WithDefaultFactory()
            => WithFactory(t => Activator.CreateInstance(t));

        #endregion

        #region UseLogger

        /// <summary>
        /// Configures the specified <see cref="ILoggerFactory" /> to be used within the bus.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns></returns>
        public BusBuilder<TBus> UseLogger(ILoggerFactory loggerFactory)
        {
            _bus.SetLoggerFactory(loggerFactory);
            return this;
        }

        #endregion
    }
}
