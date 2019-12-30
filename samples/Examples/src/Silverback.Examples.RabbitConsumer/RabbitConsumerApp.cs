// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.RabbitConsumer
{
    public class RabbitConsumerApp : ConsumerApp
    {
        protected override void ConfigureServices(IServiceCollection services) =>
            services
                .AddLogging()
                .AddSilverback()
                .AsObservable()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToRabbit(options => options
                    .AddDbLoggedInboundConnector()
                    //.AddDbOffsetStoredInboundConnector()
                    //.AddInboundConnector()
                    //.AddDbChunkStore()
                )
                .AddScopedSubscriber<SubscriberService>()
                .AddScopedBehavior<LogHeadersBehavior>();

        protected override IBroker Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator
                .Connect(endpoints => endpoints
                    .AddInbound(CreateQueueEndpoint("silverback-examples-events")));

        private static RabbitQueueConsumerEndpoint CreateQueueEndpoint(
            string name,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IMessageSerializer messageSerializer = null)
        {
            var endpoint = new RabbitQueueConsumerEndpoint(name)
            {
                Connection = GetConnectionConfig(),
                Queue = new RabbitQueueConfig
                {
                    IsDurable = durable,
                    IsExclusive = exclusive,
                    IsAutoDeleteEnabled = autoDelete
                }
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static RabbitConnectionConfig GetConnectionConfig() => new RabbitConnectionConfig
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
        };
    }
}