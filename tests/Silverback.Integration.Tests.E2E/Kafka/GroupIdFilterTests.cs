// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class GroupIdFilterTests : KafkaTestFixture
    {
        public GroupIdFilterTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task GroupIdFilterAttribute_DecoratedSubscriber_MessagesFiltered()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group2";
                                            })))
                        .AddSingletonSubscriber<DecoratedSubscriber>()
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

            var subscriber = Host.ServiceProvider.GetRequiredService<DecoratedSubscriber>();
            subscriber.ReceivedConsumer1.Should().Be(3);
            subscriber.ReceivedConsumer2.Should().Be(3);
        }

        [Fact]
        public async Task GroupIdFilterAttribute_AddedViaConfiguration_MessagesFiltered()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer2";
                                            })))
                        .AddSingletonSubscriber<Subscriber>(
                            new TypeSubscriptionOptions
                            {
                                Filters = new[]
                                {
                                    new KafkaGroupIdFilterAttribute("consumer1")
                                }
                            })
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

            var subscriber = Host.ServiceProvider.GetRequiredService<Subscriber>();
            subscriber.Received.Should().Be(3);
        }

        [Fact]
        public async Task GroupIdFilterAttribute_DelegateSubscriber_MessagesFiltered()
        {
            int received1 = 0;
            int received2 = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer2";
                                            })))
                        .AddDelegateSubscriber(
                            (IEvent _) => Interlocked.Increment(ref received1),
                            new SubscriptionOptions
                            {
                                Filters = new[]
                                {
                                    new KafkaGroupIdFilterAttribute("consumer1")
                                }
                            })
                        .AddDelegateSubscriber(
                            (IEvent _) => Interlocked.Increment(ref received2),
                            new SubscriptionOptions
                            {
                                Filters = new[]
                                {
                                    new KafkaGroupIdFilterAttribute("consumer2")
                                }
                            })
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

            received1.Should().Be(3);
            received2.Should().Be(3);
        }

        [Fact]
        public async Task GroupIdFilterAttribute_BatchSubscribedAsStream_MessagesFiltered()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group2";
                                            })))
                        .AddSingletonSubscriber<StreamSubscriber>()
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

            var subscriber = Host.ServiceProvider.GetRequiredService<StreamSubscriber>();
            subscriber.ReceivedConsumer1.Should().Be(3);
            subscriber.ReceivedConsumer2.Should().Be(3);
        }

        [UsedImplicitly]
        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class Subscriber
        {
            private int _received;

            public int Received => _received;

            [SuppressMessage(
                "ReSharper",
                "UnusedMember.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage(
                "ReSharper",
                "UnusedParameter.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void OnMessageReceived(IMessage message) =>
                Interlocked.Increment(ref _received);
        }

        [UsedImplicitly]
        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class DecoratedSubscriber
        {
            private int _receivedConsumer1;

            private int _receivedConsumer2;

            public int ReceivedConsumer1 => _receivedConsumer1;

            public int ReceivedConsumer2 => _receivedConsumer2;

            [KafkaGroupIdFilter("group1")]
            [SuppressMessage(
                "ReSharper",
                "UnusedMember.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage(
                "ReSharper",
                "UnusedParameter.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void OnConsumer1Received(IMessage message) =>
                Interlocked.Increment(ref _receivedConsumer1);

            [KafkaGroupIdFilter("group2")]
            [SuppressMessage(
                "ReSharper",
                "UnusedMember.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage(
                "ReSharper",
                "UnusedParameter.Local",
                Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void OnConsumer2Received(IMessage message) =>
                Interlocked.Increment(ref _receivedConsumer2);
        }

        [UsedImplicitly]
        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class StreamSubscriber
        {
            private int _receivedConsumer1;

            private int _receivedConsumer2;

            public int ReceivedConsumer1 => _receivedConsumer1;

            public int ReceivedConsumer2 => _receivedConsumer2;

            [KafkaGroupIdFilter("group1")]
            [SuppressMessage(
                "ReSharper",
                "UnusedMember.Local",
                Justification = Justifications.CalledBySilverback)]
            public Task OnConsumer1Received(IAsyncEnumerable<IMessage> messages) =>
                messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer1));

            [KafkaGroupIdFilter("group2")]
            [SuppressMessage(
                "ReSharper",
                "UnusedMember.Local",
                Justification = Justifications.CalledBySilverback)]
            public Task OnConsumer2Received(IAsyncEnumerable<IMessage> messages) =>
                messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer2));
        }
    }
}
