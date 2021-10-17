// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class BrokerTests
    {
        [Fact]
        public void GetProducer_SomeEndpoint_ProducerIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_SameEndpoint_SameInstanceIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(TestProducerEndpoint.GetDefault());
            var producer2 = broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(TestProducerEndpoint.GetDefault());
            var producer2 = broker.GetProducer(new TestProducerEndpoint("test2"));

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void AddConsumer_SomeEndpoint_ConsumerIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = broker.AddConsumer(TestConsumerEndpoint.GetDefault());

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void AddConsumer_SameEndpoint_DifferentInstanceIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            var consumer2 = broker.AddConsumer(new TestConsumerEndpoint("test2"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            var consumer2 = broker.AddConsumer(new TestConsumerEndpoint("test2"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public async Task ConnectAsync_WithEndpointConfigurators_InvokedOnce()
        {
            var configurator1 = Substitute.For<IEndpointsConfigurator>();
            var configurator2 = Substitute.For<IEndpointsConfigurator>();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddBroker<TestOtherBroker>())
                    .AddEndpointsConfigurator(_ => configurator1)
                    .AddEndpointsConfigurator(_ => configurator2));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();

            await broker.ConnectAsync();
            await otherBroker.ConnectAsync();
            await broker.ConnectAsync();
            await otherBroker.ConnectAsync();

            configurator1.Received(1).Configure(Arg.Any<IEndpointsConfigurationBuilder>());
            configurator2.Received(1).Configure(Arg.Any<IEndpointsConfigurationBuilder>());
        }

        [Fact]
        public async Task ConnectAsync_WithEndpointConfigurators_EndpointsAreAdded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddBroker<TestOtherBroker>())
                    .AddEndpointsConfigurator<TestConfiguratorOne>()
                    .AddEndpointsConfigurator<TestConfiguratorTwo>());

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();

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
            public void Configure(IEndpointsConfigurationBuilder builder)
            {
                builder.AddInbound(TestConsumerEndpoint.GetDefault());
            }
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "ClassNeverInstantiated.Local",
            Justification = Justifications.CalledBySilverback)]
        private sealed class TestConfiguratorTwo : IEndpointsConfigurator
        {
            public void Configure(IEndpointsConfigurationBuilder builder)
            {
                builder.AddInbound(TestOtherConsumerEndpoint.GetDefault());
            }
        }
    }
}
