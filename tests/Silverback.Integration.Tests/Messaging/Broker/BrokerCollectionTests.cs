// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerCollectionTests
{
    private readonly ProducerConfiguration[] _producerConfigurations =
    {
        TestProducerConfiguration.GetDefault(),
        TestOtherProducerConfiguration.GetDefault()
    };

    private readonly ConsumerConfiguration[] _consumerConfigurations =
    {
        TestConsumerConfiguration.GetDefault(),
        TestOtherConsumerConfiguration.GetDefault()
    };

    [Theory]
    [InlineData(0, "TestProducer")]
    [InlineData(1, "TestOtherProducer")]
    public void GetProducer_WithMultipleBrokers_RightProducerInstanceReturned(int endpointIndex, string expectedProducerType)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()));

        IBrokerCollection brokerCollection = serviceProvider.GetRequiredService<IBrokerCollection>();
        ProducerConfiguration endpoint = _producerConfigurations[endpointIndex];

        IProducer producer = brokerCollection.GetProducer(endpoint);

        producer.Should().NotBeNull();
        producer.GetType().Name.Should().BeEquivalentTo(expectedProducerType);
    }

    [Theory]
    [InlineData(0, "TestConsumer")]
    [InlineData(1, "TestOtherConsumer")]
    public void AddConsumer_WithMultipleBrokers_RightConsumerInstanceReturned(
        int endpointIndex,
        string expectedConsumerType)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()));

        IBrokerCollection brokerCollection = serviceProvider.GetRequiredService<IBrokerCollection>();
        ConsumerConfiguration endpoint = _consumerConfigurations[endpointIndex];

        IConsumer consumer = brokerCollection.AddConsumer(endpoint);

        consumer.Should().NotBeNull();
        consumer.GetType().Name.Should().BeEquivalentTo(expectedConsumerType);
    }

    [Fact]
    public async Task ConnectAsyncAndDisconnectAsync_WithMultipleBrokers_AllBrokersConnectedAndDisconnected()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()));

        IBrokerCollection brokerCollection = serviceProvider.GetRequiredService<IBrokerCollection>();

        brokerCollection[0].IsConnected.Should().BeFalse();
        brokerCollection[1].IsConnected.Should().BeFalse();

        await brokerCollection.ConnectAsync();

        brokerCollection[0].IsConnected.Should().BeTrue();
        brokerCollection[1].IsConnected.Should().BeTrue();

        await brokerCollection.DisconnectAsync();

        brokerCollection[0].IsConnected.Should().BeFalse();
        brokerCollection[1].IsConnected.Should().BeFalse();
    }
}
