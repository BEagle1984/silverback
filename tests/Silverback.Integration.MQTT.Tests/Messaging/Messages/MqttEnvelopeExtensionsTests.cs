// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttEnvelopeExtensionsTests
{
    [Fact]
    public void GetMqttResponseTopic_ShouldReturnResponseTopicForInboundEnvelope()
    {
        MqttInboundEnvelope<object, object> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));
        envelope.SetResponseTopic("topic/1");

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetMqttResponseTopic_ShouldReturnResponseTopicForOutboundEnvelope()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));
        envelope.SetResponseTopic("topic/1");

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetMqttResponseTopic_ShouldReturnNullForInboundEnvelope_WhenResponseTopicNotSet()
    {
        MqttInboundEnvelope<object, object> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBeNull();
    }

    [Fact]
    public void GetMqttResponseTopic_ShouldReturnNullForOutboundEnvelope_WhenResponseTopicNotSet()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBeNull();
    }

    [Fact]
    public void SetMqttResponseTopic_ShouldSetResponseTopic()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttResponseTopic("topic/1");

        envelope.ResponseTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldReturnCorrelationDataForInboundEnvelope()
    {
        MqttInboundEnvelope<object, int> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));
        envelope.SetCorrelationData(123);

        int? correlationData = envelope.GetMqttCorrelationData<int>();

        correlationData.ShouldBe(123);
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldReturnCorrelationDataForOutboundEnvelope()
    {
        MqttOutboundEnvelope<TestEventOne, int> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));
        envelope.SetCorrelationData(123);

        int? correlationData = envelope.GetMqttCorrelationData<int>();

        correlationData.ShouldBe(123);
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldReturnNullForInboundEnvelope_WhenCorrelationDataNotSet()
    {
        MqttInboundEnvelope<object, int?> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        envelope.SetCorrelationData(null);
        int? correlationData = envelope.GetMqttCorrelationData<int?>();

        correlationData.ShouldBeNull();
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldReturnNullForOutboundEnvelope_WhenCorrelationDataNotSet()
    {
        MqttOutboundEnvelope<TestEventOne, int?> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        int? correlationData = envelope.GetMqttCorrelationData<int?>();

        correlationData.ShouldBeNull();
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldThrowForInboundEnvelope_WhenTypeMismatch()
    {
        MqttInboundEnvelope<object, int> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));
        envelope.SetCorrelationData(123);

        Action act = () => envelope.GetMqttCorrelationData<string>();

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IMqttInboundEnvelope");
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldThrowForOutboundEnvelope_WhenTypeMismatch()
    {
        MqttOutboundEnvelope<TestEventOne, int> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));
        envelope.SetCorrelationData(123);

        Action act = () => envelope.GetMqttCorrelationData<string>();

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IMqttOutboundEnvelope");
    }

    [Fact]
    public void SetMqttRawCorrelationData_ShouldSetRawCorrelationData()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttRawCorrelationData([1, 2, 3, 4]);

        envelope.RawCorrelationData.ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public void SetMqttCorrelationData_ShouldSetCorrelationData()
    {
        MqttOutboundEnvelope<TestEventOne, int> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttCorrelationData(123);

        envelope.CorrelationData.ShouldBe(123);
    }

    [Fact]
    public void SetMqttCorrelationData_ShouldThrow_WhenTypeMismatch()
    {
        MqttOutboundEnvelope<TestEventOne, int> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        Action act = () => envelope.SetMqttCorrelationData("string");

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IMqttOutboundEnvelope");
    }

    [Fact]
    public void GetMqttDestinationTopic_ShouldReturnDestinationTopic()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));
        envelope.SetDestinationTopic("topic/1");

        string? destinationTopic = envelope.GetMqttDestinationTopic();

        destinationTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void SetMqttDestinationTopic_ShouldSetDestinationTopic()
    {
        MqttOutboundEnvelope<TestEventOne, byte[]> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttDestinationTopic("topic/1");

        envelope.DynamicDestinationTopic.ShouldBe("topic/1");
    }
}
