using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    public class BrokerOptionsBuilder
    {
        private readonly IServiceCollection _services;

        public BrokerOptionsBuilder(IServiceCollection services)
        {
            _services = services;
        }
        
        #region Serializer

        public BrokerOptionsBuilder UseSerializer<T>() where T : class, IMessageSerializer
        {
            _services.AddSingleton<IMessageSerializer, T>();
            return this;
        }

        public BrokerOptionsBuilder SerializeAsJson() => UseSerializer<JsonMessageSerializer>();

        #endregion

        #region Inbound/Outbound Connector

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// </summary>
        public BrokerOptionsBuilder AddInboundConnector()
        {
            _services.AddSingleton<IInboundConnector, InboundConnector>();
            return this;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptionsBuilder AddLoggedInboundConnector<TLog>() where TLog : class, IInboundLog
        {
            _services.AddSingleton<IInboundConnector, LoggedInboundConnector>();
            _services.AddScoped<IInboundLog, TLog>(); // TODO: Check this (scoped vs. singleton)
            return this;
        }

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        public BrokerOptionsBuilder AddOutboundConnector()
        {
            _services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
            _services.AddScoped<ISubscriber, OutboundConnector>();
            return this;
        }

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// This implementation stores the outbound messages into an intermediate queue.
        /// </summary>
        public BrokerOptionsBuilder AddDeferredOutboundConnector<TQueueProducer>() where TQueueProducer : class, IOutboundQueueProducer
        {
            _services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
            _services.AddScoped<ISubscriber, DeferredOutboundConnector>();
            _services.AddScoped<IOutboundQueueProducer, TQueueProducer>();
            return this;
        }

        #endregion

        #region Defaults

        internal void CompleteWithDefaults() => SetDefaults();

        /// <summary>
        /// Sets the default values for the options that have not been explicitely set
        /// by the user.
        /// </summary>
        protected virtual void SetDefaults()
        {
            if (_services.All(s => s.ServiceType != typeof(IMessageSerializer)))
                SerializeAsJson();

            if (_services.All(s => s.ServiceType != typeof(IInboundConnector)))
                AddInboundConnector();

            if (_services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
                AddOutboundConnector();
        }

        #endregion
    }
}