// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeCustomHeaders()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventWithHeaders>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<object>(endpoint => endpoint
                .ProduceTo(DefaultTopicName)
                .AddHeader<TestEventWithHeaders>("x-content", envelope => envelope.Message?.Content)
                .AddHeader<TestEventOne>("x-content-nope", envelope => envelope.Message?.ContentEventOne)
                .AddHeader("x-static", 42)));

        await producer.ProduceAsync(
            new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        MessageHeaderCollection headers = Helper.Spy.InboundEnvelopes.Single().Headers;

        headers.ShouldContain(new MessageHeader("x-content", "Hello E2E!"));
        headers.ShouldContain(new MessageHeader("x-static", "42"));
        headers.ShouldContain(new MessageHeader("x-custom-header", "Hello header!"));
        headers.ShouldContain(new MessageHeader("x-custom-header2", "False"));
        headers.Select(header => header.Name).ShouldNotContain("x-content-nope");
    }
}
