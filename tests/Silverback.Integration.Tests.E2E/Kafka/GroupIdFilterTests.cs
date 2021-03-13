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
        public async Task GroupIdFilterAttribute_BasicUsage_MessagesFiltered()
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
                        .AddSingletonSubscriber<Subscriber>()
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

            var subscriber = Host.ServiceProvider.GetRequiredService<Subscriber>();
            subscriber.ReceivedConsumer1.Should().Be(3);
            subscriber.ReceivedConsumer2.Should().Be(3);
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
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer2";
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
        private class Subscriber
        {
            private int _receivedConsumer1;

            private int _receivedConsumer2;

            public int ReceivedConsumer1 => _receivedConsumer1;

            public int ReceivedConsumer2 => _receivedConsumer2;

            [KafkaGroupIdFilter("consumer1")]
            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void OnConsumer1Received(IMessage message) =>
                Interlocked.Increment(ref _receivedConsumer1);

            [KafkaGroupIdFilter("consumer2")]
            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void OnConsumer2Received(IMessage message) =>
                Interlocked.Increment(ref _receivedConsumer2);
        }

        [UsedImplicitly]
        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class StreamSubscriber
        {
            private int _receivedConsumer1;

            private int _receivedConsumer2;

            public int ReceivedConsumer1 => _receivedConsumer1;

            public int ReceivedConsumer2 => _receivedConsumer2;

            [KafkaGroupIdFilter("consumer1")]
            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            public Task OnConsumer1Received(IAsyncEnumerable<IMessage> messages) =>
                messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer1));

            [KafkaGroupIdFilter("consumer2")]
            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            public Task OnConsumer2Received(IAsyncEnumerable<IMessage> messages) =>
                messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer2));
        }
    }
}
