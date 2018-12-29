// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Serialization;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class ConsumerServiceA : ConsumerService
    {
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<KafkaBroker>(options => options
                .AddDbInboundConnector<ExamplesDbContext>()
                .AddInboundConnector())
            .AddScoped<ISubscriber, SubscriberService>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider)
        {
            ConfigureNLog(serviceProvider);

            endpoints
                .AddInbound(CreateConsumerEndpoint("silverback-examples-events"))
                .AddInbound(CreateConsumerEndpoint("silverback-examples-bad-events"), policy => policy
                    .Chain(
                        policy.Retry(TimeSpan.FromMilliseconds(500)).MaxFailedAttempts(2),
                        policy.Move(new KafkaProducerEndpoint("silverback-examples-bad-events-error")
                        {
                            Configuration = new Confluent.Kafka.ProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://kafka:9092",
                                ClientId = "consumer-service-a",
                                GroupId = "silverback-examples"
                            }
                        })))
                .AddInbound(CreateConsumerEndpoint("silverback-examples-custom-serializer", GetCustomSerializer()))
                // Special inbound (not logged)
                .AddInbound<InboundConnector>(CreateConsumerEndpoint("silverback-examples-legacy-messages", new LegacyMessageSerializer()))
                .Connect();
        }

        private static KafkaConsumerEndpoint CreateConsumerEndpoint(string name, IMessageSerializer messageSerializer = null)
        {
            var endpoint = new KafkaConsumerEndpoint(name)
            {
                ReuseConsumer = true,
                Configuration = new Confluent.Kafka.ConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = "consumer-service-a",
                    GroupId = "silverback-examples"
                }
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static JsonMessageSerializer GetCustomSerializer()
        {
            var serializer = new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

            return serializer;
        }

        private static void ConfigureNLog(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");
        }
    }
}