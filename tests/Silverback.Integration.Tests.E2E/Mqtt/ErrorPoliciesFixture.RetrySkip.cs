// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10).ThenSkip()))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultClientSession.PendingMessagesCount.Should().Be(0);
    }
}
