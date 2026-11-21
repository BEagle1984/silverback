// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
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

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeTypedHeaders_WhenUsingNonInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            await Host.ConfigureServicesAndRunAsync(services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume<TestEventWithTypedHeaders>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

            IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
            Guid guid = Guid.NewGuid();
            TimeSpan timeSpan = new(1, 2, 3, 4, 5);
            DateTime dateTime = new(2023, 6, 23, 2, 42, 42, 123, DateTimeKind.Utc);
            DateTimeOffset dateTimeOffset = new(2023, 6, 23, 2, 42, 42, 123, TimeSpan.FromHours(2));

            await producer.ProduceAsync(
                new TestEventWithTypedHeaders
                {
                    NullableInt = 42,
                    NullableDouble = 1234.5,
                    NullableDecimal = 1234.5m,
                    NullableBool = true,
                    GuidValue = guid,
                    NullableGuid = guid,
                    TimeSpan = timeSpan,
                    NullableTimeSpan = timeSpan,
                    Enum = TestEventWithTypedHeaders.HeaderOption.Second,
                    NullableEnum = TestEventWithTypedHeaders.HeaderOption.Second,
                    DateTime = dateTime,
                    DateTimeOffset = dateTimeOffset,
                    NullableDateTimeOffset = dateTimeOffset
                });
            await producer.ProduceAsync(new TestEventWithTypedHeaders());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

            List<TestEventWithTypedHeaders> messages = [
                .. Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message.ShouldBeOfType<TestEventWithTypedHeaders>())
            ];

            // The messages can be consumed in any order, since they are spread across the partitions
            TestEventWithTypedHeaders message = messages.Single(typedMessage => typedMessage.GuidValue == guid);
            message.NullableInt.ShouldBe(42);
            message.NullableDouble.ShouldBe(1234.5);
            message.NullableDecimal.ShouldBe(1234.5m);
            message.NullableBool.ShouldBe(true);
            message.GuidValue.ShouldBe(guid);
            message.NullableGuid.ShouldBe(guid);
            message.TimeSpan.ShouldBe(timeSpan);
            message.NullableTimeSpan.ShouldBe(timeSpan);
            message.Enum.ShouldBe(TestEventWithTypedHeaders.HeaderOption.Second);
            message.NullableEnum.ShouldBe(TestEventWithTypedHeaders.HeaderOption.Second);
            message.DateTime.Kind.ShouldBe(DateTimeKind.Utc);
            message.DateTime.Ticks.ShouldBe(dateTime.Ticks);
            message.DateTimeOffset.ShouldBe(dateTimeOffset);
            message.DateTimeOffset.Offset.ShouldBe(dateTimeOffset.Offset);
            message.NullableDateTimeOffset.ShouldBe(dateTimeOffset);

            TestEventWithTypedHeaders emptyMessage = messages.Single(typedMessage => typedMessage.GuidValue == Guid.Empty);
            emptyMessage.NullableInt.ShouldBeNull();
            emptyMessage.NullableDouble.ShouldBeNull();
            emptyMessage.NullableDecimal.ShouldBeNull();
            emptyMessage.NullableBool.ShouldBeNull();
            emptyMessage.NullableGuid.ShouldBeNull();
            emptyMessage.NullableTimeSpan.ShouldBeNull();
            emptyMessage.NullableEnum.ShouldBeNull();
            emptyMessage.NullableDateTimeOffset.ShouldBeNull();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldSetKafkaKeyHeader()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { Content = "Hello E2E!", KafkaKey = i * 100 });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes[0].Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.MessageKey, "100"));
        Helper.Spy.InboundEnvelopes[0].GetKafkaKey().ShouldBe("100");
        Helper.Spy.InboundEnvelopes[1].Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.MessageKey, "200"));
        Helper.Spy.InboundEnvelopes[1].GetKafkaKey().ShouldBe("200");
        Helper.Spy.InboundEnvelopes[2].Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.MessageKey, "300"));
        Helper.Spy.InboundEnvelopes[2].GetKafkaKey().ShouldBe("300");
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldSetTimestampHeader()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Single().Headers.ShouldContain(header => header.Name == KafkaMessageHeaders.Timestamp);
        DateTime timestamp = Helper.Spy.InboundEnvelopes.Single().Headers.GetValueOrDefault<DateTime>(KafkaMessageHeaders.Timestamp);
        timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        timestamp.ShouldBeLessThan(DateTime.UtcNow);
        timestamp.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-1));
    }
}
