// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class TestingHelperTests
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    [Fact]
    public async Task GetProducerForEndpoint_ShouldEncrypt_WhenConsumerDecryptsUsingAes()
    {
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
                        .DecryptUsingAes(AesEncryptionKey))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "encrypted" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().RawMessage.ShouldBeOfType<SymmetricEncryptStream>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("encrypted");
    }
}
