// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    private const string MoveTargetTopicName = "other-topic";

    [Fact]
    public async Task StopPolicy_ShouldStopConsumerAndDisconnect_WhenCommitFailsAfterConsumerIsCommitted()
    {
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();

        // The first commit fails after the consumer was committed, as when a Committed handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1, isFailingAfterConsumerCommit: true);

        // The default error policy (stop) applies. Produce via a separate client, which is not disconnected when the consumer stops.
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("producer")
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()))
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS())))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker));

        void HandleMessage(TestEventOne message)
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message.ContentEventOne!);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The transaction can't be rolled back anymore: the consumer is just stopped and disconnected, as for a batch, instead of consuming
        // the next messages
        await AsyncTestingUtil.WaitAsync(
            () => consumer.Client.Status == ClientStatus.Disconnected && consumer.StatusInfo.Status == ConsumerStatus.Stopped,
            TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(["1"]);
        transactionEventsTracker.Events.ShouldBe(["Committing", "Committed"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);

        // The commit exception is logged once and rethrown, which logs it as fatal, instead of the failure of the rollback
        logEventsCollector.Count(IntegrationLogEvents.ProcessingConsumedMessageError.EventId).ShouldBe(1);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeTrue();
        logEventsCollector.ContainsProcessingFatalError(exception =>
                exception?.GetBaseException() is not InvalidOperationException { Message: "Commit failed." })
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("retry")]
    [InlineData("skip")]
    [InlineData("move")]
    public async Task ErrorPolicy_ShouldNotBeApplied_WhenCommitFailsAfterConsumerIsCommitted(string policy)
    {
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();
        IStopConsumerErrorPolicyCallback callback = (IStopConsumerErrorPolicyCallback)Substitute.For(
            [
                typeof(IStopConsumerErrorPolicyCallback),
                typeof(IRetryErrorPolicyCallback),
                typeof(ISkipMessageErrorPolicyCallback),
                typeof(IMoveMessageErrorPolicyCallback)
            ],
            []);

        // The first commit fails after the consumer was committed, as when a Committed handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1, isFailingAfterConsumerCommit: true);

        // Produce via a separate client, which is not disconnected when the consumer stops. The move producer is used by the move policy
        // only: the published messages aren't routed to it.
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("producer")
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()))
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventTwo>(endpoint => endpoint
                        .ProduceTo(MoveTargetTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(callback));

        void HandleMessage(TestEventOne message)
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message.ContentEventOne!);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The transaction can't be rolled back anymore: the policy isn't applied, since retrying, skipping or moving the message requires
        // rolling it back, and the consumer is stopped and disconnected, as with the stop policy
        await AsyncTestingUtil.WaitAsync(
            () => consumer.Client.Status == ClientStatus.Disconnected && consumer.StatusInfo.Status == ConsumerStatus.Stopped,
            TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(["1"]);
        transactionEventsTracker.Events.ShouldBe(["Committing", "Committed"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
        Helper.GetMessages(MoveTargetTopicName).Count.ShouldBe(0);

        // Only the stop callbacks are invoked
        await callback.Received(1).OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        callback.ReceivedCalls().Count().ShouldBe(1);

        // The commit exception is logged once and rethrown, which logs it as fatal, instead of the failure of the rollback
        logEventsCollector.Count(IntegrationLogEvents.ProcessingConsumedMessageError.EventId).ShouldBe(1);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToRetryFailed.EventId).ShouldBe(0);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToSkipFailed.EventId).ShouldBe(0);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeTrue();
        logEventsCollector.ContainsProcessingFatalError(exception =>
                exception?.GetBaseException() is not InvalidOperationException { Message: "Commit failed." })
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("retry")]
    [InlineData("skip")]
    [InlineData("move")]
    public async Task ErrorPolicy_ShouldBeApplied_WhenCommitFailsBeforeConsumerIsCommitted(string policy)
    {
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();

        // The first commit fails before the consumer is committed, as when a Committing handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1);

        // Produce via a separate client, which is not disconnected when the consumer stops. The move producer is used by the move policy
        // only: the published messages aren't routed to it.
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("producer")
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()))
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventTwo>(endpoint => endpoint
                        .ProduceTo(MoveTargetTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker));

        void HandleMessage(TestEventOne message)
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message.ContentEventOne!);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        if (policy == "stop")
        {
            // The transaction is rolled back and the consumer is stopped and disconnected, without acknowledging the message
            await AsyncTestingUtil.WaitAsync(
                () => consumer.Client.Status == ClientStatus.Disconnected && transactionEventsTracker.Events.Contains("Aborted"),
                TimeSpan.FromSeconds(10));

            consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
            receivedMessages.ShouldBe(["1"]);
            transactionEventsTracker.Events.ShouldBe(["Committing", "Aborting", "Aborted"]);
            DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
        }
        else
        {
            // The message is retried (consuming it again), skipped or moved
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
            receivedMessages.ShouldBe(policy == "retry" ? ["1", "1", "2", "3"] : ["1", "2", "3"]);
            transactionEventsTracker.Events.Take(3).ShouldBe(["Committing", "Aborting", "Aborted"]);
            transactionEventsTracker.Events.Count(transactionEvent => transactionEvent == "Aborted").ShouldBe(1);
            DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
            logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
        }

        Helper.GetMessages(MoveTargetTopicName).Count.ShouldBe(policy == "move" ? 1 : 0);
        logEventsCollector.Count(IntegrationLogEvents.ProcessingConsumedMessageError.EventId).ShouldBe(1);
    }
}
