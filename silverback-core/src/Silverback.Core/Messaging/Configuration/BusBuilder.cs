using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Silverback.Messaging.Configuration
{
    public abstract class BusBuilder<TBus>
        where TBus : IBus
    {
        private readonly TBus _bus;

        protected BusBuilder() => _bus = CreateInstance();

        protected abstract TBus CreateInstance();

        /// <summary>
        /// Builds and returns the <see cref="IBus"/> instance.
        /// </summary>
        public TBus Build()
        {
            if (_bus.GetTypeFactory() == null)
                WithDefaultFactory();

            if (_bus.GetLoggerFactory() == null)
                _bus.SetLoggerFactory(NullLoggerFactory.Instance);

            return _bus;
        }

        /// <summary>
        /// Sets the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        public BusBuilder<TBus> WithFactory(Func<Type, object> singleInstanceFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(singleInstanceFactory));
            return this;
        }

        /// <summary>
        /// Sets the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        public BusBuilder<TBus> WithFactory(Func<Type, IEnumerable<object>> multiInstancesFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(multiInstancesFactory));
            return this;
        }

        /// <summary>
        /// Sets the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        public BusBuilder<TBus> WithFactory(Func<Type, object> singleInstanceFactory, Func<Type, IEnumerable<object>> multiInstancesFactory)
        {
            _bus.SetTypeFactory(new GenericTypeFactory(singleInstanceFactory, multiInstancesFactory));
            return this;
        }

        /// <summary>
        /// Sets the <see cref="ITypeFactory" /> to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
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
        public BusBuilder<TBus> WithDefaultFactory() => WithFactory(t => Activator.CreateInstance(t));

        /// <summary>
        /// Configures the specified <see cref="ILoggerFactory" /> to be used within the bus.
        /// </summary>
        public BusBuilder<TBus> UseLogger(ILoggerFactory loggerFactory)
        {
            _bus.SetLoggerFactory(loggerFactory);
            return this;
        }
    }

    public class BusBuilder : BusBuilder<Bus>
    {
        protected override Bus CreateInstance()
            => new Bus();
    }
}
