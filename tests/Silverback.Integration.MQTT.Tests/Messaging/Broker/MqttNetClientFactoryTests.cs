// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Diagnostics;
using NSubstitute;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker;

public class MqttNetClientFactoryTests
{
    private readonly MqttBroker _broker;

    private readonly ISilverbackLogger<MqttClientsCache> _logger;

    public MqttNetClientFactoryTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
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
        MqttClientConfiguration configuration = new()
        {
            Channel = new MqttClientTcpConfiguration
            {
                Server = "mqtt-server"
            }
        };

        MqttProducer producer1 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = configuration
            });
        MqttProducer producer2 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = configuration
            });

        MqttClientsCache factory = new(
            new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
            Substitute.For<IBrokerCallbacksInvoker>(),
            _logger);
        MqttClientWrapper client1 = factory.GetClient(producer1);
        MqttClientWrapper client2 = factory.GetClient(producer2);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client2.Should().BeSameAs(client1);
    }

    [Fact]
    public void GetClient_ProducersWithEqualClientConfig_SameClientReturned()
    {
        MqttProducer producer1 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });
        MqttProducer producer2 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });

        MqttClientsCache factory = new(
            new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
            Substitute.For<IBrokerCallbacksInvoker>(),
            _logger);
        MqttClientWrapper client1 = factory.GetClient(producer1);
        MqttClientWrapper client2 = factory.GetClient(producer2);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client2.Should().BeSameAs(client1);
    }

    [Fact]
    public void GetClient_ProducerAndConsumerWithEqualClientConfig_SameClientReturned()
    {
        MqttProducer producer = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });
        MqttConsumer consumer = (MqttConsumer)_broker.AddConsumer(
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });

        MqttClientsCache factory = new(
            new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
            Substitute.For<IBrokerCallbacksInvoker>(),
            _logger);
        MqttClientWrapper client1 = factory.GetClient(producer);
        MqttClientWrapper client2 = factory.GetClient(consumer);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client2.Should().BeSameAs(client1);
    }

    [Fact]
    public void GetClient_ConsumerAndProducerWithEqualClientConfig_SameClientReturned()
    {
        MqttConsumer consumer = (MqttConsumer)_broker.AddConsumer(
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });
        MqttProducer producer = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });

        MqttClientsCache factory = new(
            new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
            Substitute.For<IBrokerCallbacksInvoker>(),
            _logger);
        MqttClientWrapper client1 = factory.GetClient(producer);
        MqttClientWrapper client2 = factory.GetClient(consumer);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client2.Should().BeSameAs(client1);
    }

    [Fact]
    public void GetClient_ConsumersWithEquivalentClientConfig_ExceptionThrown()
    {
        _broker.AddConsumer(
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });

        Action act = () => _broker.AddConsumer(
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
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
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });
        Action act = () => _broker.AddConsumer(
            new MqttConsumerConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>(new[] { "some-topic" }),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
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
        MqttProducer producer1 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });
        MqttProducer producer2 = (MqttProducer)_broker.GetProducer(
            new MqttProducerConfiguration
            {
                Endpoint = new MqttStaticProducerEndpointResolver("some-topic"),
                Client = new MqttClientConfiguration
                {
                    ClientId = "client2",
                    Channel = new MqttClientTcpConfiguration
                    {
                        Server = "mqtt-server"
                    }
                }
            });

        MqttClientsCache factory = new(
            new MqttNetClientFactory(Substitute.For<IMqttNetLogger>()),
            Substitute.For<IBrokerCallbacksInvoker>(),
            _logger);
        MqttClientWrapper client1 = factory.GetClient(producer1);
        MqttClientWrapper client2 = factory.GetClient(producer2);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client2.Should().NotBeSameAs(client1);
    }
}
