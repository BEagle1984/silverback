// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerTests
{
    [Fact]
    public void GetProducer_SomeEndpoint_ProducerReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IProducer producer = broker.GetProducer(TestProducerConfiguration.GetDefault());

        producer.Should().NotBeNull();
    }

    [Fact]
    public void GetProducer_SameEndpoint_SameInstanceReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IProducer producer = broker.GetProducer(TestProducerConfiguration.GetDefault());
        IProducer producer2 = broker.GetProducer(TestProducerConfiguration.GetDefault());

        producer2.Should().BeSameAs(producer);
    }

    [Fact]
    public void GetProducer_DifferentEndpoint_DifferentInstanceReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IProducer producer = broker.GetProducer(TestProducerConfiguration.GetDefault());
        IProducer producer2 = broker.GetProducer(new TestProducerConfiguration("test2"));

        producer2.Should().NotBeSameAs(producer);
    }

    [Fact]
    public void AddConsumer_SomeEndpoint_ConsumerReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IConsumer consumer = broker.AddConsumer(TestConsumerConfiguration.GetDefault());

        consumer.Should().NotBeNull();
    }

    [Fact]
    public void AddConsumer_SameEndpoint_DifferentInstanceReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IConsumer consumer = broker.AddConsumer(TestConsumerConfiguration.GetDefault());
        IConsumer consumer2 = broker.AddConsumer(new TestConsumerConfiguration("test2"));

        consumer2.Should().NotBeSameAs(consumer);
    }

    [Fact]
    public void AddConsumer_DifferentEndpoint_DifferentInstanceReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()));

        IBroker broker = serviceProvider.GetRequiredService<IBroker>();
        IConsumer consumer = broker.AddConsumer(TestConsumerConfiguration.GetDefault());
        IConsumer consumer2 = broker.AddConsumer(new TestConsumerConfiguration("test2"));

        consumer2.Should().NotBeSameAs(consumer);
    }

    [Fact]
    public async Task ConnectAsync_WithEndpointConfigurators_InvokedOnce()
    {
        IEndpointsConfigurator? configurator1 = Substitute.For<IEndpointsConfigurator>();
        IEndpointsConfigurator? configurator2 = Substitute.For<IEndpointsConfigurator>();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>())
                .AddEndpointsConfigurator(_ => configurator1)
                .AddEndpointsConfigurator(_ => configurator2));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        TestOtherBroker otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();

        await broker.ConnectAsync();
        await otherBroker.ConnectAsync();
        await broker.ConnectAsync();
        await otherBroker.ConnectAsync();

        configurator1.Received(1).Configure(Arg.Any<EndpointsConfigurationBuilder>());
        configurator2.Received(1).Configure(Arg.Any<EndpointsConfigurationBuilder>());
    }

    [Fact]
    public async Task ConnectAsync_WithEndpointConfigurators_EndpointsAreAdded()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>())
                .AddEndpointsConfigurator<TestConfiguratorOne>()
                .AddEndpointsConfigurator<TestConfiguratorTwo>());

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        TestOtherBroker otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();

        await broker.ConnectAsync();
        await otherBroker.ConnectAsync();

        broker.Consumers.Should().HaveCount(1);
        otherBroker.Consumers.Should().HaveCount(1);
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage(
        "ReSharper",
        "ClassNeverInstantiated.Local",
        Justification = Justifications.CalledBySilverback)]
    private sealed class TestConfiguratorOne : IEndpointsConfigurator
    {
        public void Configure(EndpointsConfigurationBuilder builder)
        {
            builder.AddInbound(TestConsumerConfiguration.GetDefault());
        }
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage(
        "ReSharper",
        "ClassNeverInstantiated.Local",
        Justification = Justifications.CalledBySilverback)]
    private sealed class TestConfiguratorTwo : IEndpointsConfigurator
    {
        public void Configure(EndpointsConfigurationBuilder builder)
        {
            builder.AddInbound(TestOtherConsumerConfiguration.GetDefault());
        }
    }
}
