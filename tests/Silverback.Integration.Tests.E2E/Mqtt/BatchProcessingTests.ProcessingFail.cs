// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldStopConsumerWithoutAcknowledging_WhenProcessingFails()
    {
        int receivedMessages = 0;

        // Produce via a separate client, which is not disconnected when the consumer stops
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne unused in batch)
            {
                if (Interlocked.Increment(ref receivedMessages) == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        receivedMessages.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_ShouldRollBackAndStopConsumerWithoutAcknowledging_WhenErrorHandlingFails(bool isCompletedByTimeout)
    {
        TransactionEventsTracker transactionEventsTracker = new();

        // The error handling fails, since the callback invoked by the skip error policy throws
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));

        // Produce via a separate client, which is not disconnected when the consumer stops
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(isCompletedByTimeout ? 10 : 3, TimeSpan.FromMilliseconds(300))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(callback));

        // When the batch is completed by its max wait time, the subscriber fails while no message is being added
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                if (!isCompletedByTimeout && message.ContentEventOne == "2")
                    throw new InvalidOperationException("Test");
            }

            throw new InvalidOperationException("Test");
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The batch transaction is rolled back, raising the Aborting and Aborted events, as when the error policies don't handle the error
        await AsyncTestingUtil.WaitAsync(
            () => consumer.Client.Status == ClientStatus.Disconnected && transactionEventsTracker.Events.Contains("Aborted"),
            TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        transactionEventsTracker.Events.ShouldBe(["Aborting", "Aborted"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
        await callback.Received(1).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("retry")]
    [InlineData("skip")]
    public async Task Batch_ShouldApplyErrorPolicy_WhenCommitFailsAfterEnumerationIsAborted(string policy)
    {
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();

        // The first commit fails, as when a Committing handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1);

        // Produce via a separate client, which is not disconnected when the consumer stops
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromSeconds(10))
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonBrokerBehavior(transactionEventsTracker));

        // The subscriber stops enumerating each batch after its first message: the next messages are added to the next batch
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message.ContentEventOne!);
                }

                break;
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
            // The batch transaction is rolled back after the failed commit, raising the Aborting and Aborted events, and the consumer is
            // stopped and disconnected without acknowledging the messages, as when the commit of a completed batch fails
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
            // The batch is retried (consuming its message again) or skipped, as when the commit of a completed batch fails
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
            receivedMessages.ShouldBe(policy == "retry" ? ["1", "1", "2", "3"] : ["1", "2", "3"]);
            transactionEventsTracker.Events.Take(3).ShouldBe(["Committing", "Aborting", "Aborted"]);
            transactionEventsTracker.Events.Count(transactionEvent => transactionEvent == "Aborted").ShouldBe(1);
            DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
            logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
        }

        // The commit exception is logged once, as when the commit of a completed batch fails
        logEventsCollector.Count(IntegrationLogEvents.SequenceProcessingError.EventId).ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_ShouldStopConsumerAndDisconnect_WhenCommitFailsAfterConsumerIsCommitted(bool isCompletedByTimeout)
    {
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();

        // The first commit fails after the consumer was committed, as when a Committed handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1, isFailingAfterConsumerCommit: true);

        // Produce via a separate client, which is not disconnected when the consumer stops
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(isCompletedByTimeout ? 10 : 3, TimeSpan.FromMilliseconds(300)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonBrokerBehavior(transactionEventsTracker));

        // When the batch is completed by its max wait time, no message is being added to it when its commit fails, so no reader rethrows
        // the exception
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message.ContentEventOne!);
                }
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The transaction can't be rolled back anymore: the consumer is just stopped and disconnected, since the stop error policy doesn't
        // handle the error, instead of consuming the next messages
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(["1", "2", "3"]);
        transactionEventsTracker.Events.ShouldBe(["Committing", "Committed"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        // The commit exception is logged and rethrown to the message completing the batch (if any), which logs it as fatal, instead of the
        // failure of the rollback
        logEventsCollector.Count(IntegrationLogEvents.SequenceProcessingError.EventId).ShouldBe(1);
        logEventsCollector.ContainsProcessingFatalError(exception =>
                exception?.GetBaseException() is not InvalidOperationException { Message: "Commit failed." })
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("retry", "size")]
    [InlineData("retry", "timeout")]
    [InlineData("retry", "break")]
    [InlineData("skip", "size")]
    [InlineData("skip", "timeout")]
    [InlineData("skip", "break")]
    public async Task Batch_ShouldStopConsumerWithoutApplyingErrorPolicy_WhenCommitFailsAfterConsumerIsCommitted(
        string policy,
        string completion)
    {
        bool isCompletedByTimeout = completion == "timeout";
        bool isEnumerationAborted = completion == "break";
        List<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();
        IStopConsumerErrorPolicyCallback callback = (IStopConsumerErrorPolicyCallback)Substitute.For(
            [typeof(IStopConsumerErrorPolicyCallback), typeof(IRetryErrorPolicyCallback), typeof(ISkipMessageErrorPolicyCallback)],
            []);

        // The first commit fails after the consumer was committed, as when a Committed handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1, isFailingAfterConsumerCommit: true);

        // Produce via a separate client, which is not disconnected when the consumer stops
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(
                            isCompletedByTimeout ? 10 : 3,
                            isCompletedByTimeout ? TimeSpan.FromMilliseconds(300) : TimeSpan.FromSeconds(10))
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(callback));

        // The subscriber that stops enumerating the batch after its first message makes the abort of its enumeration commit the batch, while
        // no message is being added to the batch completed by its max wait time when its commit fails
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message.ContentEventOne!);
                }

                if (isEnumerationAborted)
                    break;
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= (isCompletedByTimeout ? 3 : 6); i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The transaction can't be rolled back anymore: the policy isn't applied, since retrying or skipping the batch requires rolling it
        // back, and the consumer is stopped and disconnected, as with the stop policy and as for a single message
        await AsyncTestingUtil.WaitAsync(
            () => consumer.Client.Status == ClientStatus.Disconnected && consumer.StatusInfo.Status == ConsumerStatus.Stopped,
            TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(isEnumerationAborted ? ["1"] : ["1", "2", "3"]);
        transactionEventsTracker.Events.ShouldBe(["Committing", "Committed"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(isCompletedByTimeout ? 0 : isEnumerationAborted ? 5 : 3);

        // Only the stop callbacks are invoked
        await callback.Received(1).OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        callback.ReceivedCalls().Count().ShouldBe(1);

        // The commit exception is logged once and rethrown to the message completing the batch (none when the batch is completed by its max
        // wait time), which logs it as fatal, instead of the failure of the rollback
        logEventsCollector.Count(IntegrationLogEvents.SequenceProcessingError.EventId).ShouldBe(1);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToRetryFailed.EventId).ShouldBe(0);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToSkipFailed.EventId).ShouldBe(0);
        logEventsCollector.Count(IntegrationLogEvents.ProcessingConsumedMessageFatalError.EventId).ShouldBe(isCompletedByTimeout ? 0 : 1);
        logEventsCollector.ContainsProcessingFatalError(exception =>
                exception?.GetBaseException() is not InvalidOperationException { Message: "Commit failed." })
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_ShouldStopConsumerAndDisconnectWithoutAcknowledging_WhenBatchCompletedByTimeoutFails(bool isErrorHandlingFailed)
    {
        int stopCalls = 0;

        // When the error handling fails (the stop callback throws), the rollback doesn't stop the consumer
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return isErrorHandlingFailed ? Task.FromException(new InvalidOperationException("Callback failed.")) : Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(300)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // No message is being added to the batch when the subscriber fails, after enumerating the whole batch completed by its max wait
        // time, so no reader rethrows the exception
        static async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne unused in batch)
            {
                // Enumerate the whole batch before failing
            }

            throw new InvalidOperationException("Test");
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
    }

    [Fact]
    public async Task Batch_ShouldNotStopRestartedConsumer_WhenBatchCompletedByTimeoutFailsAfterRestart()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstBatchEnumerated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> failFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(300)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        // The first batch, completed by its max wait time, keeps being processed while the consumer is restarted (ignoring the
        // cancellation), and only then fails
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            if (Interlocked.Increment(ref batchesCount) == 1)
            {
                firstBatchEnumerated.TrySetResult(true);
                await failFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
                throw new InvalidOperationException("Test");
            }

            successfulBatches.Add(list);
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });

        await firstBatchEnumerated.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await consumer.StopAsync(false);
        await consumer.StartAsync();
        failFirstBatch.TrySetResult(true);

        // The stop requested by the stale error handling is ignored and the restarted consumer processes the messages again
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        successfulBatches.ShouldHaveSingleItem().ShouldBe(["1", "2"]);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldStartNextBatchOnlyAfterFailedBatchIsRolledBack()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> retrying = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseRetry = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        // Delays the rollback of the failed batch, while the next message is consumed
        IRetryErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback>();
        callback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                retrying.TrySetResult(true);
                return releaseRetry.Task.WaitAsync(TimeSpan.FromSeconds(30));
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails once
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // The batch started by the next message when the reader restarts is aborted
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
                throw new InvalidOperationException("Retry!");

            successfulBatches.Add(list);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await retrying.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next batch isn't started while the error policies are applied to the failed one
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count > 1, TimeSpan.FromSeconds(2));
        receivedBatches.Count.ShouldBe(1);

        releaseRetry.TrySetResult(true);

        // The failed batch is replayed, followed by the next message
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Sum(batch => batch.Count) >= 3);
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldNotAcknowledgeNextBatch_WhileFailedBatchCompletedByTimeoutIsHandled()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseStop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        // Delays the error handling of the failed batch, while the next messages are consumed
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                stopping.TrySetResult(true);
                return releaseStop.Task.WaitAsync(TimeSpan.FromSeconds(30));
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(2, TimeSpan.FromSeconds(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails, stopping the consumer
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // The batch started after the failed one is aborted by the stop
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
                throw new InvalidOperationException("Stop!");

            successfulBatches.Add(list);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await stopping.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next messages would complete the next batch, which must not be acknowledged before the failed one is handled
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        // The stop policy then stops the consumer before the next batch is completed
        releaseStop.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldRetryMessage_WhenDeserializationFailsWhileBatchWithIgnoredMessagesIsPending()
    {
        int retriesCount = 0;
        int skipsCount = 0;
        IErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback, ISkipMessageErrorPolicyCallback>();
        ((IRetryErrorPolicyCallback)callback).OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retriesCount);
                return Task.CompletedTask;
            });
        ((ISkipMessageErrorPolicyCallback)callback).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref skipsCount);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages()
                        .OnError(policy => policy.Retry(2).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        static async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                // Just consume the batch
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The unhandled message starts the batch without being added to it, then a message fails before the sequencer
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);

        await AsyncTestingUtil.WaitAsync(() => skipsCount > 0);

        retriesCount.ShouldBe(2);
        skipsCount.ShouldBe(1);

        // The batch started by the ignored message is completed and every delivery is acknowledged
        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldSkipBatchWithoutMoving_WhenMovePolicyCannotHandleSequences()
    {
        int tryCount = 0;
        IMoveMessageErrorPolicyCallback callback = Substitute.For<IMoveMessageErrorPolicyCallback, ISkipMessageErrorPolicyCallback>();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.MoveTo("other-topic").ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne unused in batch)
            {
                if (Interlocked.Increment(ref tryCount) == 3)
                    throw new InvalidOperationException("Skip!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The move policy cannot handle sequences, so the failed batch falls through to the skip (not replayed) and is acknowledged
        tryCount.ShouldBe(3);
        await callback.DidNotReceive().OnMovingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        await ((ISkipMessageErrorPolicyCallback)callback).Received(1).OnSkippingMessageAsync(
            Arg.Any<IRawInboundEnvelope>(),
            Arg.Any<Exception>());
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumerWithoutAcknowledging_WhenRetriesAreExhausted()
    {
        int tryCount = 0;

        // Produce via a separate client, which is not disconnected when the consumer stops
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
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
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne unused in batch)
            {
                // Fail every attempt
                Interlocked.Increment(ref tryCount);
                throw new InvalidOperationException("Test");
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        // The batch is attempted once and retried once, then the default stop policy applies
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        tryCount.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldReplayBatchToAllSubscribers_WhenOneSubscriberFails()
    {
        TestingCollection<string?> receivedByFirst = [];
        TestingCollection<string?> receivedBySecond = [];
        int secondMessageAttempts = 0;
        IRetryErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback>();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(FailingSubscriberAsync)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(SecondSubscriberAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task FailingSubscriberAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedByFirst.Add(message.ContentEventOne);

                // Fail the first attempt only
                if (message.ContentEventOne == "2" && Interlocked.Increment(ref secondMessageAttempts) == 1)
                    throw new InvalidOperationException("Retry!");
            }
        }

        async Task SecondSubscriberAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedBySecond.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The retry policy replays the batch to both subscribers: the failing one fails at the second message, then succeeds
        await callback.Received(1).OnRetryingMessageAsync(
            Arg.Any<IRawInboundEnvelope>(),
            Arg.Is<Exception>(exception => exception.Message == "Retry!"));
        secondMessageAttempts.ShouldBe(2);
        receivedByFirst.ShouldBe(["1", "2", "1", "2", "3"]);
        receivedBySecond.TakeLast(3).ShouldBe(["1", "2", "3"]);
        receivedBySecond.Count.ShouldBeGreaterThan(3);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    private static void ConfigureErrorPolicy(IErrorPolicyBuilder errorPolicy, string policy)
    {
        switch (policy)
        {
            case "retry":
                errorPolicy.Retry(1);
                break;
            case "skip":
                errorPolicy.Skip();
                break;
            default:
                errorPolicy.Stop();
                break;
        }
    }
}
