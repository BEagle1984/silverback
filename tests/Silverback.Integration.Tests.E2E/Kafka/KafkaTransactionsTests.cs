// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class KafkaTransactionsTests : KafkaTestFixture
    {
        public KafkaTransactionsTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task KafkaTransactionalProducer_MessagesProducedAfterCommit()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions
                                        .WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.TransactionalId = "transaction-id";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group-id";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            KafkaTransactionalProducer producer = Helper.Broker.GetTransactionalProducer(DefaultTopicName);

            producer.BeginTransaction();

            await producer.ProduceAsync(new TestEventOne());
            await producer.ProduceAsync(new TestEventTwo());
            await producer.ProduceAsync(new TestEventThree());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

            producer.CommitTransaction();

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).Should()
                .BeEquivalentTo(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventThree() });
        }

        [Fact]
        public async Task KafkaTransactionalProducer_NoMessageProducedWhenAborted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions
                                        .WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.TransactionalId = "transaction-id";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group-id";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            KafkaTransactionalProducer producer = Helper.Broker.GetTransactionalProducer(DefaultTopicName);

            producer.BeginTransaction();

            await producer.ProduceAsync(new TestEventOne());
            await producer.ProduceAsync(new TestEventTwo());
            await producer.ProduceAsync(new TestEventThree());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

            producer.AbortTransaction();

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);
        }

        [Fact]
        public async Task KafkaTransactionalProducer_MessagesProducedWithoutTransaction()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions
                                        .WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.TransactionalId = "transaction-id";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "group-id";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            KafkaTransactionalProducer producer = Helper.Broker.GetTransactionalProducer(DefaultTopicName);

            await producer.ProduceAsync(new TestEventOne());
            await producer.ProduceAsync(new TestEventTwo());
            await producer.ProduceAsync(new TestEventThree());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).Should()
                .BeEquivalentTo(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventThree() });
        }
    }
}
