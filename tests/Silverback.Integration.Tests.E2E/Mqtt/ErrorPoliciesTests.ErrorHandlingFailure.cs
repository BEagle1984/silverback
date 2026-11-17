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

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    [Theory]
    [InlineData("move failed")]
    [InlineData("error policy callback failed")]
    [InlineData("stop callback failed")]
    public async Task ErrorPolicy_ShouldRollBackAndStopConsumerWithoutAcknowledging_WhenErrorHandlingFails(string scenario)
    {
        TransactionEventsTracker transactionEventsTracker = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
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
            consumer.Client.Status == ClientStatus.Disconnected && transactionEventsTracker.Events.Contains("Aborted"));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        transactionEventsTracker.Events.ShouldBe(["Aborting", "Aborted"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);
        Helper.GetMessages("other-topic").Count.ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
    }

    // The move policy isn't covered, since it doesn't handle sequences (chunked messages included)
    [Theory]
    [InlineData("error policy callback failed")]
    [InlineData("stop callback failed")]
    public async Task ErrorPolicy_ShouldRollBackAndStopConsumerWithoutAcknowledging_WhenErrorHandlingFailsForChunkedMessage(string scenario)
    {
        TransactionEventsTracker transactionEventsTracker = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(policy => ConfigureFailingErrorPolicy(policy, scenario)))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(CreateFailingErrorPolicyCallback(scenario))
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Processing failed.");

        IProducer producer = Helper.GetProducer(client => client
            .ConnectViaTcp("e2e-mqtt-broker")
            .Produce<TestEventOne>(endpoint => endpoint
                .ProduceTo(DefaultTopicName)
                .WithAtLeastOnceQoS()
                .EnableChunking(10)));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Long message one" });

        int chunksCount = Helper.GetMessages(DefaultTopicName).Count;
        chunksCount.ShouldBeGreaterThan(1);

        // The chunk sequence transaction is rolled back, raising the Aborting and Aborted events, as when the error policies don't
        // handle the error
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() =>
            consumer.Client.Status == ClientStatus.Disconnected && transactionEventsTracker.Events.Contains("Aborted"));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        transactionEventsTracker.Events.ShouldBe(["Aborting", "Aborted"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(chunksCount);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
    }

    private static void ConfigureFailingErrorPolicy(IErrorPolicyBuilder errorPolicy, string scenario)
    {
        switch (scenario)
        {
            case "move failed":
                errorPolicy.MoveTo(
                    "other-topic",
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
