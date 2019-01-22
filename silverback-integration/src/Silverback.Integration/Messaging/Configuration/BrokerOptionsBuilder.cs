// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    public class BrokerOptionsBuilder
    {
        internal readonly IServiceCollection Services;

        public BrokerOptionsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// </summary>
        public BrokerOptionsBuilder AddInboundConnector<TConnector>() where TConnector : class, IInboundConnector
        {
            Services.AddSingleton<IInboundConnector, TConnector>();
            return this;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// </summary>
        public BrokerOptionsBuilder AddInboundConnector() => AddInboundConnector<InboundConnector>();

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptionsBuilder AddLoggedInboundConnector<TLog>() where TLog : class, IInboundLog
        {
            AddInboundConnector<LoggedInboundConnector>();
            Services.AddScoped<IInboundLog, TLog>();
            return this;
        }

        
        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation stores the offset of the latest consumed messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptionsBuilder AddOffsetStoredInboundConnector<TStore>() where TStore : class, IOffsetStore
        {
            AddInboundConnector<OffsetStoredInboundConnector>();
            Services.AddScoped<IOffsetStore, TStore>();
            return this;
        }

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        public BrokerOptionsBuilder AddOutboundConnector<TConnector>() where TConnector : class, IOutboundConnector
        {
            if (Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
            {
                Services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
                Services.AddScoped<ISubscriber, OutboundConnectorRouter>();
            }

            Services.AddScoped<IOutboundConnector, TConnector>();

            return this;
        }

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        public BrokerOptionsBuilder AddOutboundConnector() => AddOutboundConnector<OutboundConnector>();

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// This implementation stores the outbound messages into an intermediate queue.
        /// </summary>
        public BrokerOptionsBuilder AddDeferredOutboundConnector<TQueueProducer>() where TQueueProducer : class, IOutboundQueueProducer
        {
            AddOutboundConnector<DeferredOutboundConnector>();
            Services.AddScoped<ISubscriber, DeferredOutboundConnector>();
            Services.AddScoped<IOutboundQueueProducer, TQueueProducer>();

            return this;
        }

        internal BrokerOptionsBuilder AddOutboundWorker(bool enforceMessageOrder, int readPackageSize)
        {
            Services.AddScoped(s => new OutboundQueueWorker(
                s.GetRequiredService<IOutboundQueueConsumer>(), 
                s.GetRequiredService<IBroker>(), 
                s.GetRequiredService<ILogger<OutboundQueueWorker>>(),
                s.GetRequiredService<MessageLogger>(),
                enforceMessageOrder, readPackageSize));

            return this;
        }

        /// <summary>
        /// Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="enforceMessageOrder">if set to <c>true</c> the message order will be preserved (no message will be skipped).</param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        // TODO: Test
        public BrokerOptionsBuilder AddOutboundWorker<TQueueConsumer>(bool enforceMessageOrder = true, int readPackageSize = 100) where TQueueConsumer : class, IOutboundQueueConsumer
        {
            AddOutboundWorker(enforceMessageOrder, readPackageSize);
            Services.AddScoped<IOutboundQueueConsumer, TQueueConsumer>();

            return this;
        }

        #region Defaults

        internal void CompleteWithDefaults() => SetDefaults();

        /// <summary>
        /// Sets the default values for the options that have not been explicitely set
        /// by the user.
        /// </summary>
        protected virtual void SetDefaults()
        {
            if (Services.All(s => s.ServiceType != typeof(IInboundConnector)))
                AddInboundConnector();

            if (Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
                AddOutboundConnector();
        }

        #endregion
    }
}