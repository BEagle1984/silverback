// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    [Theory]
    [InlineData("skip")]
    [InlineData("stop")]
    [InlineData("retry")]
    public async Task ErrorPolicy_ShouldNotBeApplied_WhenStopCancelsProcessing(string policy)
    {
        int tryCount = 0;
        TaskCompletionSource<bool> processingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISkipMessageErrorPolicyCallback callback =
            Substitute.For<ISkipMessageErrorPolicyCallback, IRetryErrorPolicyCallback, IStopConsumerErrorPolicyCallback>();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<IIntegrationEvent, CancellationToken>(HandleMessageAsync)
            .AddSingletonErrorPolicyCallback(callback)
            .AddIntegrationSpy());

        async Task HandleMessageAsync(IIntegrationEvent message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref tryCount) > 1)
                return;

            processingStarted.TrySetResult(true);

            // The first attempt is canceled by the stop
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The blocking stop waits for the canceled processing
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        callback.ReceivedCalls().ShouldBeEmpty();
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromSeconds(10));

        // Processed again and acknowledged once
        tryCount.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[1].Headers.Contains(DefaultMessageHeaders.FailedAttempts).ShouldBeFalse();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
        callback.ReceivedCalls().ShouldBeEmpty();
    }

    private static void ConfigureErrorPolicy(IErrorPolicyBuilder errorPolicy, string policy)
    {
        switch (policy)
        {
            case "skip":
                errorPolicy.Skip();
                break;
            case "stop":
                errorPolicy.Stop();
                break;
            default:
                errorPolicy.Retry(10);
                break;
        }
    }
}
