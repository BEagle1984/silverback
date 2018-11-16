using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Must be implemented 
    /// </summary>
    public class BrokerOptions
    {
        private readonly IServiceCollection _services;

        public BrokerOptions(IServiceCollection services)
        {
            _services = services;
        }
        
        #region Serializer

        public BrokerOptions UseSerializer<T>() where T : class, IMessageSerializer
        {
            _services.AddSingleton<IMessageSerializer, T>();
            return this;
        }

        public BrokerOptions SerializeAsJson() => UseSerializer<JsonMessageSerializer>();

        #endregion

        #region Inbound/Outbound Connector

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// </summary>
        public BrokerOptions AddInboundConnector()
        {
            _services.AddSingleton<IInboundConnector, InboundConnector>();
            return this;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptions AddLoggedInboundConnector<TLog>() where TLog : class, IInboundLog
        {
            _services.AddSingleton<IInboundConnector, LoggedInboundConnector>();
            _services.AddSingleton<IInboundLog, TLog>();
            return this;
        }
        
        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// </summary>
        public BrokerOptions AddOutboundConnector()
        {
            _services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
            _services.AddSingleton<ISubscriber, OutboundConnector>();
            return this;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptions AddDeferredOutboundConnector<TQueueWriter>() where TQueueWriter : class, IOutboundQueueWriter
        {
            _services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
            _services.AddSingleton<ISubscriber, DeferredOutboundConnector>();
            _services.AddSingleton<IOutboundQueueWriter, TQueueWriter>();
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
        }

        #endregion
    }
}