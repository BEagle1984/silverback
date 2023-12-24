// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Transactions;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class KafkaTransactionsFixture : KafkaFixture
{
    public KafkaTransactionsFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task KafkaTransactions_ShouldProduceInTransaction()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        using (IKafkaTransaction transaction = publisher.InitKafkaTransaction())
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            // Not committed yet, so we expect no message to be consumed
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

            transaction.Commit();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
    }

    [Fact]
    public async Task KafkaTransactions_ShouldRollbackProduce()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        using (IKafkaTransaction unused = publisher.InitKafkaTransaction())
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            // No commit means an implicit abort
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);
    }
}
