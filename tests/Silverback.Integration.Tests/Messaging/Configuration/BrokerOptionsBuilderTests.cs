// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    [Collection("StaticInMemory")]
    public class BrokerOptionsBuilderTests
    {
        private readonly IServiceCollection _services;
        private readonly TestSubscriber _testSubscriber;
        private IServiceProvider _serviceProvider;
        private IServiceScope _serviceScope;

        private IServiceProvider GetServiceProvider() => _serviceProvider ??=
            _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        private IServiceProvider GetScopedServiceProvider() =>
            (_serviceScope ??= GetServiceProvider().CreateScope()).ServiceProvider;

        private TestBroker GetBroker() => (TestBroker) GetServiceProvider().GetService<IBroker>();
        private IPublisher GetPublisher() => GetScopedServiceProvider().GetService<IPublisher>();

        private BusConfigurator GetBusConfigurator() => GetServiceProvider().GetService<BusConfigurator>();

        private InMemoryOutboundQueue GetOutboundQueue() =>
            (InMemoryOutboundQueue) GetScopedServiceProvider().GetService<IOutboundQueueProducer>();

        public BrokerOptionsBuilderTests()
        {
            _testSubscriber = new TestSubscriber();

            _services = new ServiceCollection();

            _services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _serviceProvider = null; // Creation deferred to after AddBroker() has been called
            _serviceScope = null;

            InMemoryInboundLog.Clear();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void AddOutboundConnector_PublishMessages_MessagesProduced()
        {
            _services.AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddOutboundConnector())
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints => endpoints
                .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault()));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());

            GetBroker().ProducedMessages.Count.Should().Be(5);
        }

        [Fact]
        public async Task AddDeferredOutboundConnector_PublishMessages_MessagesQueued()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddDeferredOutboundConnector(_ => new InMemoryOutboundQueue()))
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints => endpoints
                .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault()));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCompletedEvent());

            (await GetOutboundQueue().GetLength()).Should().Be(5);
        }

        [Fact]
        public async Task AddDeferredOutboundConnector_Rollback_MessagesNotQueued()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddDeferredOutboundConnector(_ => new InMemoryOutboundQueue()))
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints => endpoints
                .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault()));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCompletedEvent());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionAbortedEvent());

            (await GetOutboundQueue().GetLength()).Should().Be(2);
        }

        [Fact]
        public async Task AddInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddInboundConnector())
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints => endpoints
                .AddInbound(TestConsumerEndpoint.GetDefault()));

            var consumer = GetBroker().Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }

        [Fact]
        public async Task AddInboundConnector_CalledMultipleTimes_EachMessageReceivedOnce()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddInboundConnector()
                    .AddInboundConnector())
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints => endpoints
                .AddInbound(TestConsumerEndpoint.GetDefault()));

            var consumer = GetBroker().Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }

        [Fact]
        public async Task AddLoggedInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddLoggedInboundConnector(s =>
                        new InMemoryInboundLog(s.GetRequiredService<MessageIdProvider>())))
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(TestConsumerEndpoint.GetDefault()));

            var consumer = GetBroker().Consumers.First();
            var duplicatedId = Guid.NewGuid();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = duplicatedId });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = duplicatedId });

            _testSubscriber.ReceivedMessages.Count.Should().Be(4);
        }

        [Fact]
        public async Task AddOffsetStoredInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddOffsetStoredInboundConnector(_ => new InMemoryOffsetStore()))
                .AddSingletonSubscriber(_testSubscriber);
            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(TestConsumerEndpoint.GetDefault()));

            var consumer = GetBroker().Consumers.First();
            await consumer.TestPush(
                new TestEventOne { Id = Guid.NewGuid() },
                offset: new TestOffset("test-1", "1"));
            await consumer.TestPush(
                new TestEventTwo { Id = Guid.NewGuid() },
                offset: new TestOffset("test-2", "1"));
            await consumer.TestPush(
                new TestEventOne { Id = Guid.NewGuid() },
                offset: new TestOffset("test-1", "2"));
            await consumer.TestPush(
                new TestEventTwo { Id = Guid.NewGuid() },
                offset: new TestOffset("test-2", "1"));
            await consumer.TestPush(
                new TestEventOne { Id = Guid.NewGuid() },
                offset: new TestOffset("test-1", "3"));
            await consumer.TestPush(
                new TestEventTwo { Id = Guid.NewGuid() },
                offset: new TestOffset("test-2", "2"));

            _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }
    }
}