// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics;

public class IntegrationLoggerExtensionsFixture
{
    private readonly LoggerSubstitute<IntegrationLoggerExtensionsFixture> _loggerSubstitute;

    private readonly ISilverbackLogger<IntegrationLoggerExtensionsFixture> _silverbackLogger;

    public IntegrationLoggerExtensionsFixture()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker());

        _loggerSubstitute =
            (LoggerSubstitute<IntegrationLoggerExtensionsFixture>)serviceProvider
                .GetRequiredService<ILogger<IntegrationLoggerExtensionsFixture>>();

        _silverbackLogger = serviceProvider
            .GetRequiredService<ISilverbackLogger<IntegrationLoggerExtensionsFixture>>();
    }

    [Fact]
    public void LogConsumerFatalError_ShouldLog()
    {
        _silverbackLogger.LogConsumerFatalError(new TestConsumer(), new InvalidCastException());

        _loggerSubstitute.Received(
            LogLevel.Critical,
            typeof(InvalidCastException),
            "Fatal error occurred processing the consumed message. The consumer will be stopped. | consumerName: consumer1",
            1004);
    }

    [Fact]
    public void LogMessageAddedToSequence_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        _silverbackLogger.LogMessageAddedToSequence(envelope, new FakeSequence());

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Message '1234' added to FakeSequence 'fake1'. | length: 3",
            1011);
    }

    [Fact]
    public void LogSequenceStarted_ShouldLog()
    {
        _silverbackLogger.LogSequenceStarted(new FakeSequence());

        _loggerSubstitute.Received(LogLevel.Debug, null, "Started new FakeSequence 'fake1'.", 1012);
    }

    [Fact]
    public void LogSequenceCompleted_ShouldLog()
    {
        _silverbackLogger.LogSequenceCompleted(new FakeSequence());

        _loggerSubstitute.Received(LogLevel.Debug, null, "FakeSequence 'fake1' completed. | length: 3", 1013);
    }

    [Fact]
    public void LogSequenceAborted_ShouldLog()
    {
        _silverbackLogger.LogSequenceAborted(new FakeSequence(), SequenceAbortReason.ConsumerAborted);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "The FakeSequence 'fake1' processing has been aborted. | length: 3, reason: ConsumerAborted",
            1014);
    }

    [Fact]
    public void LogSequenceProcessingError_ShouldLog()
    {
        _silverbackLogger.LogSequenceProcessingError(new FakeSequence(), new InvalidDataException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidDataException),
            "Error occurred processing the FakeSequence 'fake1'. | length: 3",
            1015);
    }

    [Fact]
    public void LogIncompleteSequenceAborted_ShouldLog()
    {
        _silverbackLogger.LogIncompleteSequenceAborted(new FakeSequence());

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Aborted incomplete FakeSequence 'fake1'. | length: 3",
            1016);
    }

    [Fact]
    public void LogIncompleteSequenceSkipped_ShouldLog()
    {
        _silverbackLogger.LogIncompleteSequenceSkipped(new IncompleteSequence("fake1", ConsumerPipelineContextHelper.CreateSubstitute()));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Skipped incomplete sequence 'fake1'. The first message is missing.",
            1017);
    }

    [Fact]
    public void LogSequenceTimeoutError_ShouldLog()
    {
        _silverbackLogger.LogSequenceTimeoutError(new FakeSequence(), new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(TimeoutException),
            "Error occurred executing the timeout for the FakeSequence 'fake1'.",
            1018);
    }

    [Fact]
    public void LogBrokerClientsInitializationError_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientsInitializationError(new AuthenticationException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(AuthenticationException),
            "Error occurred initializing the broker client(s).",
            1021);
    }

    [Fact]
    public void LogBrokerClientInitializing_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientInitializing(new TestClient());

        _loggerSubstitute.Received(LogLevel.Debug, null, "TestClient initializing... | clientName: client1", 1022);
    }

    [Fact]
    public void LogBrokerClientInitialized_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientInitialized(new TestClient());

        _loggerSubstitute.Received(LogLevel.Debug, null, "TestClient initialized. | clientName: client1", 1023);
    }

    [Fact]
    public void LogBrokerClientDisconnecting_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientDisconnecting(new TestClient());

        _loggerSubstitute.Received(LogLevel.Debug, null, "TestClient disconnecting... | clientName: client1", 1024);
    }

    [Fact]
    public void LogBrokerClientDisconnected_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientDisconnected(new TestClient());

        _loggerSubstitute.Received(LogLevel.Information, null, "TestClient disconnected. | clientName: client1", 1025);
    }

    [Fact]
    public void LogBrokerClientInitializeError_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientInitializeError(new TestClient(), new ArgumentNullException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(ArgumentNullException),
            "Error occurred initializing TestClient. | clientName: client1",
            1026);
    }

    [Fact]
    public void LogBrokerClientDisconnectError_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientDisconnectError(new TestClient(), new ArgumentNullException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(ArgumentNullException),
            "Error occurred disconnecting TestClient. | clientName: client1",
            1027);
    }

    [Fact]
    public void LogBrokerClientReconnectError_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientReconnectError(new TestClient(), TimeSpan.FromMilliseconds(42), new ArgumentNullException());

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(ArgumentNullException),
            "Failed to reconnect the TestClient. Will retry in 42 milliseconds. | clientName: client1",
            1028);
    }

    [Fact]
    public void LogConsumerStartError_ShouldLog()
    {
        _silverbackLogger.LogConsumerStartError(new TestConsumer(), new InvalidCastException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidCastException),
            "Error occurred (re)starting the TestConsumer. | consumerName: consumer1",
            1031);
    }

    [Fact]
    public void LogConsumerStopError_ShouldLog()
    {
        _silverbackLogger.LogConsumerStopError(new TestConsumer(), new InvalidCastException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidCastException),
            "Error occurred stopping the TestConsumer. | consumerName: consumer1",
            1032);
    }

    [Fact]
    public void LogConsumerCommitError_ShouldLog()
    {
        _silverbackLogger.LogConsumerCommitError(
            new TestConsumer(),
            new[]
            {
                new TestOffset("a", "42"),
                new TestOffset("b", "13")
            },
            new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(TimeoutException),
            "TestConsumer commit failed. | consumerName: consumer1, identifiers: a@42, b@13",
            1033);
    }

    [Fact]
    public void LogConsumerRollbackError_ShouldLog()
    {
        _silverbackLogger.LogConsumerRollbackError(
            new TestConsumer(),
            new[]
            {
                new TestOffset("a", "42"),
                new TestOffset("b", "13")
            },
            new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(TimeoutException),
            "TestConsumer rollback failed. | consumerName: consumer1, identifiers: a@42, b@13",
            1034);
    }

    [Fact]
    public void LogBrokerClientCreated_ShouldLog()
    {
        _silverbackLogger.LogBrokerClientCreated(new TestClient());

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Created TestClient. | clientName: client1",
            1041);
    }

    [Fact]
    public void LogConsumerCreated_ShouldLog()
    {
        _silverbackLogger.LogConsumerCreated(new TestConsumer());

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Created TestConsumer. | consumerName: consumer1",
            1042);
    }

    [Fact]
    public void LogProducerCreated_ShouldLog()
    {
        _silverbackLogger.LogProducerCreated(new TestProducer());

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Created TestProducer. | producerName: producer1",
            1043);
    }

    [Fact]
    public void LogReadingMessagesFromOutbox_ShouldLog()
    {
        _silverbackLogger.LogReadingMessagesFromOutbox(42);

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Reading batch of 42 messages from the outbox queue...",
            1074);
    }

    [Fact]
    public void LogOutboxEmpty_ShouldLog()
    {
        _silverbackLogger.LogOutboxEmpty();

        _loggerSubstitute.Received(LogLevel.Trace, null, "The outbox is empty.", 1075);
    }

    [Fact]
    public void LogProcessingOutboxStoredMessage_ShouldLog()
    {
        _silverbackLogger.LogProcessingOutboxStoredMessage(13, 42);

        _loggerSubstitute.Received(LogLevel.Debug, null, "Processing outbox message 13 of 42.", 1076);
    }

    [Fact]
    public void LogErrorProducingOutboxStoredMessage_ShouldLog()
    {
        _silverbackLogger.LogErrorProducingOutboxStoredMessage(new ArithmeticException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(ArithmeticException),
            "Failed to produce the message stored in the outbox.",
            1077);
    }

    [Fact]
    public void LogErrorProcessingOutbox_ShouldLog()
    {
        _silverbackLogger.LogErrorProcessingOutbox(new InvalidCredentialException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidCredentialException),
            "Error occurred processing the outbox.",
            1078);
    }

    [Fact]
    public void LogInvalidEndpointConfiguration_ShouldLog()
    {
        _silverbackLogger.LogInvalidEndpointConfiguration(TestProducerEndpointConfiguration.GetDefault(), new BrokerConfigurationException());

        _loggerSubstitute.Received(
            LogLevel.Critical,
            typeof(BrokerConfigurationException),
            "Invalid configuration for endpoint 'test'.",
            1101);
    }

    [Fact]
    public void LogEndpointConfiguratorError_ShouldLog()
    {
        _silverbackLogger.LogEndpointConfiguratorError(
            new GenericBrokerClientsConfigurator(
                _ =>
                {
                }),
            new BrokerConfigurationException());

        _loggerSubstitute.Received(
            LogLevel.Critical,
            typeof(BrokerConfigurationException),
            "Error occurred configuring the endpoints. | configurator: GenericBrokerClientsConfigurator",
            1102);
    }

    [Fact]
    public void LogCallbackError_ShouldLog()
    {
        _silverbackLogger.LogCallbackError(new InvalidCredentialException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidCredentialException),
            "Error occurred invoking the callback handler(s).",
            1103);
    }

    [Fact]
    public void LogEndpointBuilderError_ShouldLog()
    {
        _silverbackLogger.LogEndpointBuilderError("test", new BrokerConfigurationException());

        _loggerSubstitute.Received(
            LogLevel.Critical,
            typeof(BrokerConfigurationException),
            "Failed to configure endpoint 'test'.",
            1104);
    }

    [Fact]
    public void LogLowLevelTrace_NoException_ShouldLog()
    {
        string expectedMessage = "Message A 42 True";

        _silverbackLogger.LogLowLevelTrace(
            "Message {string} {int} {bool}",
            () => new object[] { "A", 42, true });

        _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
    }

    [Fact]
    public void LogLowLevelTrace_WithException_ShouldLog()
    {
        _silverbackLogger.LogLowLevelTrace(
            new InvalidComObjectException(),
            "Message {string} {int} {bool}",
            () => new object[] { "A", 42, true });

        _loggerSubstitute.Received(
            LogLevel.Trace,
            typeof(InvalidComObjectException),
            "Message A 42 True",
            1999);
    }

    [Fact]
    public void LogConsumerLowLevelTrace_ShouldLog()
    {
        _silverbackLogger.LogConsumerLowLevelTrace(
            new TestConsumer(),
            "Message {string} {int} {bool}",
            () => new object[] { "A", 42, true });

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Message A 42 True | consumerName: consumer1",
            1999);
    }

    [Fact]
    public void LogConsumerLowLevelTrace_NoArguments_ShouldLog()
    {
        _silverbackLogger.LogConsumerLowLevelTrace(new TestConsumer(), "Message");

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Message | consumerName: consumer1",
            1999);
    }

    private class TestClient : IBrokerClient
    {
        public string Name => "client1-name";

        public string DisplayName => "client1";

        public AsyncEvent<BrokerClient> Initializing { get; } = new();

        public AsyncEvent<BrokerClient> Initialized { get; } = new();

        public AsyncEvent<BrokerClient> Disconnecting { get; } = new();

        public AsyncEvent<BrokerClient> Disconnected { get; } = new();

        public ClientStatus Status => ClientStatus.Initializing;

        public void Dispose() => throw new NotSupportedException();

        public ValueTask DisposeAsync() => throw new NotSupportedException();

        public ValueTask ConnectAsync() => throw new NotSupportedException();

        public ValueTask DisconnectAsync() => throw new NotSupportedException();

        public ValueTask ReconnectAsync() => throw new NotSupportedException();
    }

    private class TestConsumer : IConsumer
    {
        public string Name => "consumer1-name";

        public string DisplayName => "consumer1";

        public IBrokerClient Client { get; } = new TestClient();

        public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; } = Array.Empty<ConsumerEndpointConfiguration>();

        public IConsumerStatusInfo StatusInfo { get; } = new ConsumerStatusInfo();

        public ValueTask TriggerReconnectAsync() => throw new NotSupportedException();

        public ValueTask StartAsync() => throw new NotSupportedException();

        public ValueTask StopAsync() => throw new NotSupportedException();

        public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public int IncrementFailedAttempts(IRawInboundEnvelope envelope) => throw new NotSupportedException();
    }

    private class TestProducer : IProducer
    {
        public string Name => "producer1-name";

        public string DisplayName => "producer1";

        public IBrokerClient Client { get; } = new TestClient();

        public ProducerEndpointConfiguration EndpointConfiguration { get; } = TestProducerEndpointConfiguration.GetDefault();

        public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public ValueTask ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask ProduceAsync(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();
    }
}
