// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client.Options;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker
{
    public class MqttClientFactoryTests
    {
        private readonly MqttBroker _broker;

        private readonly ISilverbackLogger<MqttClientsCache> _logger;

        public MqttClientFactoryTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddMqtt()));

            _broker = serviceProvider.GetRequiredService<MqttBroker>();
            _logger = serviceProvider.GetRequiredService<ISilverbackLogger<MqttClientsCache>>();
        }

        [Fact]
        public void GetClient_ProducersWithSameClientConfig_SameClientReturned()
        {
            var config = new MqttClientConfig
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "mqtt-server"
                }
            };

            var producer1 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = config
                });
            var producer2 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = config
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer1);
            var client2 = factory.GetClient(producer2);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().BeSameAs(client1);
        }

        [Fact]
        public void GetClient_ProducersWithEqualClientConfig_SameClientReturned()
        {
            var producer1 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });
            var producer2 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer1);
            var client2 = factory.GetClient(producer2);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().BeSameAs(client1);
        }

        [Fact]
        public void GetClient_ProducerAndConsumerWithEqualClientConfigAndEventHandlers_SameClientReturned()
        {
            var eventsHandlers = new MqttEventsHandlers();

            var producer = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = eventsHandlers
                });
            var consumer = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = eventsHandlers
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer);
            var client2 = factory.GetClient(consumer);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().BeSameAs(client1);
        }

        [Fact]
        public void GetClient_ConsumerAndProducerWithEqualClientConfig_SameClientReturned()
        {
            var eventsHandlers = new MqttEventsHandlers();

            var consumer = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = eventsHandlers
                });
            var producer = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = eventsHandlers
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer);
            var client2 = factory.GetClient(consumer);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().BeSameAs(client1);
        }

        [Fact]
        public void GetClient_ConsumersWithEquivalentClientConfig_ExceptionThrown()
        {
            var consumer1 = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });
            var consumer2 = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            factory.GetClient(consumer1);
            Action act = () => factory.GetClient(consumer2);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetClient_ConsumersWithSameClientIdAndDifferentClientConfig_ExceptionThrown()
        {
            var consumer1 = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });
            var consumer2 = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server2"
                        }
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            factory.GetClient(consumer1);
            Action act = () => factory.GetClient(consumer2);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetClient_ProducersWithDifferentClientConfig_DifferentClientsReturned()
        {
            var producer1 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });
            var producer2 = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client2",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer1);
            var client2 = factory.GetClient(producer2);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().NotBeSameAs(client1);
        }

        [Fact]
        public void GetClient_ConsumerAndProducerWithDifferentEventHandlers_ExceptionThrown()
        {
            var consumer = (MqttConsumer)_broker.AddConsumer(
                new MqttConsumerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = new MqttEventsHandlers()
                });
            var producer = (MqttProducer)_broker.GetProducer(
                new MqttProducerEndpoint("some-topic")
                {
                    Configuration = new MqttClientConfig
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "mqtt-server"
                        }
                    },
                    EventsHandlers = new MqttEventsHandlers()
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(),
                _logger);
            var client1 = factory.GetClient(producer);
            Action act = () => factory.GetClient(consumer);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
