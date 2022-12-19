// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker
{
    public class MqttNetClientFactoryTests
    {
        private readonly MqttBroker _broker;

        private readonly ISilverbackLogger<MqttClientsCache> _logger;

        public MqttNetClientFactoryTests()
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
                new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
                Substitute.For<IBrokerCallbacksInvoker>(),
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
                new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
                Substitute.For<IBrokerCallbacksInvoker>(),
                _logger);
            var client1 = factory.GetClient(producer1);
            var client2 = factory.GetClient(producer2);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().BeSameAs(client1);
        }

        [Fact]
        public void GetClient_ProducerAndConsumerWithEqualClientConfig_SameClientReturned()
        {
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
                    }
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
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
                Substitute.For<IBrokerCallbacksInvoker>(),
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
                    }
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
                    }
                });

            var factory = new MqttClientsCache(
                new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
                Substitute.For<IBrokerCallbacksInvoker>(),
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
            _broker.AddConsumer(
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

            Action act = () => _broker.AddConsumer(
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

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetClient_ConsumersWithSameClientIdAndDifferentClientConfig_ExceptionThrown()
        {
            _broker.AddConsumer(
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
            Action act = () => _broker.AddConsumer(
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
                new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
                Substitute.For<IBrokerCallbacksInvoker>(),
                _logger);
            var client1 = factory.GetClient(producer1);
            var client2 = factory.GetClient(producer2);

            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client2.Should().NotBeSameAs(client1);
        }
    }
}
