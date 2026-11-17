// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesTests
{
    [Theory]
    [InlineData("move failed")]
    [InlineData("error policy callback failed")]
    [InlineData("stop callback failed")]
    public async Task ErrorPolicy_ShouldRollBackAndStopConsumer_WhenErrorHandlingFails(string scenario)
    {
        TransactionEventsTracker transactionEventsTracker = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo(MoveTargetTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => ConfigureFailingErrorPolicy(policy, scenario)))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(CreateFailingErrorPolicyCallback(scenario))
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Processing failed.");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        // The transaction is rolled back, raising the Aborting and Aborted events, as when the error policies don't handle the error
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() =>
            consumer.StatusInfo.Status == ConsumerStatus.Stopped && transactionEventsTracker.Events.Contains("Aborted"));

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        transactionEventsTracker.Events.ShouldBe(["Aborting", "Aborted"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.GetTopic(MoveTargetTopicName).MessagesCount.ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
    }

    // The move policy isn't covered, since it doesn't handle sequences (chunked messages included)
    [Theory]
    [InlineData("error policy callback failed")]
    [InlineData("stop callback failed")]
    public async Task ErrorPolicy_ShouldRollBackAndStopConsumer_WhenErrorHandlingFailsForChunkedMessage(string scenario)
    {
        TransactionEventsTracker transactionEventsTracker = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => ConfigureFailingErrorPolicy(policy, scenario)))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(CreateFailingErrorPolicyCallback(scenario))
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Processing failed.");

        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint
                .ProduceTo(DefaultTopicName)
                .EnableChunking(10)));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Long message one" });

        Helper.GetTopic(DefaultTopicName).MessagesCount.ShouldBeGreaterThan(1);

        // The chunk sequence transaction is rolled back, raising the Aborting and Aborted events, as when the error policies don't
        // handle the error
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() =>
            consumer.StatusInfo.Status == ConsumerStatus.Stopped && transactionEventsTracker.Events.Contains("Aborted"));

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        transactionEventsTracker.Events.ShouldBe(["Aborting", "Aborted"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
    }

    private static void ConfigureFailingErrorPolicy(IErrorPolicyBuilder errorPolicy, string scenario)
    {
        switch (scenario)
        {
            case "move failed":
                errorPolicy.MoveTo(
                    MoveTargetTopicName,
                    movePolicy => movePolicy.Transform(_ => throw new InvalidOperationException("Move failed.")));
                break;
            case "error policy callback failed":
                errorPolicy.Skip();
                break;
            default:
                errorPolicy.Stop();
                break;
        }
    }

    // The error handling fails, since the callback invoked by the skip error policy or when the consumer is stopped throws
    private static IErrorPolicyCallback CreateFailingErrorPolicyCallback(string scenario)
    {
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback, IStopConsumerErrorPolicyCallback>();

        if (scenario == "error policy callback failed")
        {
            callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
                .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));
        }

        if (scenario == "stop callback failed")
        {
            ((IStopConsumerErrorPolicyCallback)callback).OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
                .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));
        }

        return callback;
    }
}
