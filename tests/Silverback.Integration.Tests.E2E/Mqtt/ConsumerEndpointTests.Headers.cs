// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeHeaders()
    {
        int receivedMessages = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddIntegrationSpy());

        ValueTask HandleMessage(TestEventOne message)
        {
            receivedMessages++;
            return ValueTask.CompletedTask;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(
                new TestEventOne { ContentEventOne = $"{i}" },
                [
                    new MessageHeader("header1", $"value{i}"),
                    new MessageHeader("header2", $"value{i}")
                ]);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(10);

        for (int i = 1; i <= 10; i++)
        {
            Helper.Spy.InboundEnvelopes[i - 1].Headers.GetValue("header1").ShouldBe($"value{i}");
            Helper.Spy.InboundEnvelopes[i - 1].Headers.GetValue("header2").ShouldBe($"value{i}");
        }
    }
}
