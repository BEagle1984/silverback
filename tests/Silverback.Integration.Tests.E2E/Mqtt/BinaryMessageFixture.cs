// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class BinaryMessageFixture : MqttFixture
{
    public BinaryMessageFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BinaryMessage_ShouldProduceAndConsume()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain"
        };

        TestingCollection<byte[]?> receivedFiles = [];

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
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleBinaryMessage)
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.ShouldBeOfType<BinaryMessage>().ContentType)
            .ShouldBe(["application/pdf", "text/plain"]);

        receivedFiles.Count.ShouldBe(2);
        receivedFiles.ShouldBe(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }
}
