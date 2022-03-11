// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class IntegrationLoggerExtensionsTests
    {
        private readonly LoggerSubstitute<IntegrationLoggerExtensionsTests> _loggerSubstitute;

        private readonly ISilverbackLogger<IntegrationLoggerExtensionsTests> _silverbackLogger;

        private readonly IServiceProvider _serviceProvider;

        public IntegrationLoggerExtensionsTests()
        {
            _serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            _loggerSubstitute =
                (LoggerSubstitute<IntegrationLoggerExtensionsTests>)_serviceProvider
                    .GetRequiredService<ILogger<IntegrationLoggerExtensionsTests>>();

            _silverbackLogger = _serviceProvider
                .GetRequiredService<ISilverbackLogger<IntegrationLoggerExtensionsTests>>();
        }

        [Fact]
        public void LogMessageAddedToSequence_Logged()
        {
            var expectedMessage = "Message '1234' added to FakeSequence 'fake1'. | length: 3";

            _silverbackLogger.LogMessageAddedToSequence(
                new RawInboundEnvelope(
                    Stream.Null,
                    new MessageHeaderCollection
                    {
                        { DefaultMessageHeaders.MessageId, "1234" }
                    },
                    TestConsumerEndpoint.GetDefault(),
                    "test",
                    new TestOffset()),
                new FakeSequence());

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1003);
        }

        [Fact]
        public void LogSequenceStarted_Logged()
        {
            var expectedMessage = "Started new FakeSequence 'fake1'.";

            _silverbackLogger.LogSequenceStarted(new FakeSequence());

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1004);
        }

        [Fact]
        public void LogSequenceCompleted_Logged()
        {
            var expectedMessage = "FakeSequence 'fake1' completed. | length: 3";

            _silverbackLogger.LogSequenceCompleted(new FakeSequence());

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1005);
        }

        [Fact]
        public void LogSequenceAborted_Logged()
        {
            var expectedMessage =
                "The FakeSequence 'fake1' processing has been aborted. | " +
                "length: 3, reason: ConsumerAborted";

            _silverbackLogger.LogSequenceAborted(new FakeSequence(), SequenceAbortReason.ConsumerAborted);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1006);
        }

        [Fact]
        public void LogSequenceProcessingError_Logged()
        {
            var expectedMessage =
                "Error occurred processing the FakeSequence 'fake1'. | " +
                "length: 3";

            _silverbackLogger.LogSequenceProcessingError(new FakeSequence(), new InvalidDataException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidDataException), expectedMessage, 1007);
        }

        [Fact]
        public void LogIncompleteSequenceAborted_Logged()
        {
            var expectedMessage =
                "The incomplete FakeSequence 'fake1' is aborted. | " +
                "length: 3";

            _silverbackLogger.LogIncompleteSequenceAborted(new FakeSequence());

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1008);
        }

        [Fact]
        public void LogSkippingIncompleteSequence_Logged()
        {
            var expectedMessage = "Skipping the incomplete sequence 'fake1'. The first message is missing.";

            _silverbackLogger.LogSkippingIncompleteSequence(
                new IncompleteSequence("fake1", ConsumerPipelineContextHelper.CreateSubstitute()));

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1009);
        }

        [Fact]
        public void LogSequenceTimeoutError_Logged()
        {
            var expectedMessage = "Error occurred executing the timeout for the FakeSequence 'fake1'.";

            _silverbackLogger.LogSequenceTimeoutError(new FakeSequence(), new TimeoutException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 1110);
        }

        [Fact]
        public void LogBrokerConnecting_Logged()
        {
            var expectedMessage = "TestBroker connecting to message broker...";

            _silverbackLogger.LogBrokerConnecting(_serviceProvider.GetRequiredService<TestBroker>());

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1011);
        }

        [Fact]
        public void LogBrokerConnected_Logged()
        {
            var expectedMessage = "TestBroker connected to message broker.";

            _silverbackLogger.LogBrokerConnected(_serviceProvider.GetRequiredService<TestBroker>());

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1012);
        }

        [Fact]
        public void LogBrokerDisconnecting_Logged()
        {
            var expectedMessage = "TestBroker disconnecting from message broker...";

            _silverbackLogger.LogBrokerDisconnecting(_serviceProvider.GetRequiredService<TestBroker>());

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1013);
        }

        [Fact]
        public void LogBrokerDisconnected_Logged()
        {
            var expectedMessage = "TestBroker disconnected from message broker.";

            _silverbackLogger.LogBrokerDisconnected(_serviceProvider.GetRequiredService<TestBroker>());

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1014);
        }

        [Fact]
        public void LogCreatingNewConsumer_Logged()
        {
            var expectedMessage = "Creating new consumer for endpoint 'test'.";

            _silverbackLogger.LogCreatingNewConsumer(TestConsumerEndpoint.GetDefault());

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1015);
        }

        [Fact]
        public void LogCreatingNewProducer_Logged()
        {
            var expectedMessage = "Creating new producer for endpoint 'test'.";

            _silverbackLogger.LogCreatingNewProducer(TestProducerEndpoint.GetDefault());

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1016);
        }

        [Fact]
        public void LogBrokerConnectionError_Logged()
        {
            var expectedMessage = "Error occurred connecting to the message broker(s).";

            _silverbackLogger.LogBrokerConnectionError(new AuthenticationException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(AuthenticationException),
                expectedMessage,
                1017);
        }

        [Fact]
        public void LogConsumerConnected_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Connected consumer to endpoint. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerConnected(consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1021);
        }

        [Fact]
        public void LogConsumerDisconnected_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Disconnected consumer from endpoint. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerDisconnected(consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1022);
        }

        [Fact]
        public void LogConsumerFatalError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Fatal error occurred processing the consumed message. The consumer will be stopped. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerFatalError(consumer, new AggregateException());

            _loggerSubstitute.Received(LogLevel.Critical, typeof(AggregateException), expectedMessage, 1023);
        }

        [Fact]
        public void LogConsumerDisposingError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Error occurred while disposing the consumer. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerDisposingError(consumer, new InvalidCastException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidCastException), expectedMessage, 1024);
        }

        [Fact]
        public void LogConsumerCommitError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Commit failed. " +
                $"| consumerId: {consumer.Id}, endpointName: test, identifiers: a@42, b@13";

            _silverbackLogger.LogConsumerCommitError(
                consumer,
                new[]
                {
                    new TestOffset("a", "42"),
                    new TestOffset("b", "13")
                },
                new TimeoutException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 1025);
        }

        [Fact]
        public void LogConsumerRollbackError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Rollback failed. " +
                $"| consumerId: {consumer.Id}, endpointName: test, identifiers: a@42, b@13";

            _silverbackLogger.LogConsumerRollbackError(
                consumer,
                new[]
                {
                    new TestOffset("a", "42"),
                    new TestOffset("b", "13")
                },
                new TimeoutException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 1026);
        }

        [Fact]
        public void LogConsumerConnectError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Error occurred while connecting the consumer. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerConnectError(consumer, new InvalidCastException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidCastException), expectedMessage, 1127);
        }

        [Fact]
        public void LogConsumerDisconnectError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Error occurred while disconnecting the consumer. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerDisconnectError(consumer, new InvalidCastException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidCastException), expectedMessage, 1128);
        }

        [Fact]
        public void LogConsumerStartError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Error occurred while (re)starting the consumer. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerStartError(consumer, new InvalidCastException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidCastException), expectedMessage, 1129);
        }

        [Fact]
        public void LogConsumerStopError_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Error occurred while stopping the consumer. " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerStopError(consumer, new InvalidCastException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidCastException), expectedMessage, 1130);
        }

        [Fact]
        public void LogErrorReconnectingConsumer_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Failed to reconnect the consumer. Will retry in 42000 milliseconds. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogErrorReconnectingConsumer(
                TimeSpan.FromSeconds(42),
                consumer,
                new InvalidProgramException());

            _loggerSubstitute.Received(
                LogLevel.Warning,
                typeof(InvalidProgramException),
                expectedMessage,
                1131);
        }

        [Fact]
        public void LogProducerConnected_Logged()
        {
            var producer = _serviceProvider.GetRequiredService<TestBroker>()
                .GetProducer(TestProducerEndpoint.GetDefault());

            var expectedMessage =
                "Connected producer to endpoint. " +
                $"| producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogProducerConnected(producer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1027);
        }

        [Fact]
        public void LogProducerDisconnected_Logged()
        {
            var producer = _serviceProvider.GetRequiredService<TestBroker>()
                .GetProducer(TestProducerEndpoint.GetDefault());

            var expectedMessage =
                "Disconnected producer from endpoint. " +
                $"| producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogProducerDisconnected(producer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1028);
        }

        [Fact]
        public void LogReadingMessagesFromOutbox_Logged()
        {
            var expectedMessage = "Reading a batch of 42 messages from the outbox queue...";

            _silverbackLogger.LogReadingMessagesFromOutbox(42);

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1074);
        }

        [Fact]
        public void LogOutboxEmpty_Logged()
        {
            var expectedMessage = "The outbox is empty.";

            _silverbackLogger.LogOutboxEmpty();

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1075);
        }

        [Fact]
        public void LogProcessingOutboxStoredMessage_Logged()
        {
            var expectedMessage = "Processing outbox message 13 of 42.";

            _silverbackLogger.LogProcessingOutboxStoredMessage(13, 42);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1076);
        }

        [Fact]
        public void LogErrorProcessingOutbox_Logged()
        {
            var expectedMessage = "Error occurred processing the outbox.";

            _silverbackLogger.LogErrorProcessingOutbox(new InvalidCredentialException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(InvalidCredentialException),
                expectedMessage,
                1078);
        }

        [Fact]
        public void LogInvalidMessageProduced_Logged()
        {
            var expectedMessage =
                $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The Id field is required.";

            _silverbackLogger.LogInvalidMessageProduced($"{Environment.NewLine}- The Id field is required.");

            _loggerSubstitute.Received(
                LogLevel.Warning,
                null,
                expectedMessage,
                1079);
        }

        [Fact]
        public void LogInvalidMessageProcessed_Logged()
        {
            var expectedMessage =
                $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The Id field is required.";

            _silverbackLogger.LogInvalidMessageProcessed($"{Environment.NewLine}- The Id field is required.");

            _loggerSubstitute.Received(
                LogLevel.Warning,
                null,
                expectedMessage,
                1080);
        }

        [Fact]
        public void LogInvalidEndpointConfiguration_Logged()
        {
            var expectedMessage = "Invalid configuration for endpoint 'test'.";

            _silverbackLogger.LogInvalidEndpointConfiguration(
                TestProducerEndpoint.GetDefault(),
                new EndpointConfigurationException());

            _loggerSubstitute.Received(
                LogLevel.Critical,
                typeof(EndpointConfigurationException),
                expectedMessage,
                1101);
        }

        [Fact]
        public void LogEndpointConfiguratorError_Logged()
        {
            var expectedMessage =
                "Error occurred configuring the endpoints. | configurator: GenericEndpointsConfigurator";

            _silverbackLogger.LogEndpointConfiguratorError(
                new GenericEndpointsConfigurator(
                    _ =>
                    {
                    }),
                new EndpointConfigurationException());

            _loggerSubstitute.Received(
                LogLevel.Critical,
                typeof(EndpointConfigurationException),
                expectedMessage,
                1102);
        }

        [Fact]
        public void LogCallbackHandlerError_Logged()
        {
            var expectedMessage =
                "Error occurred invoking the callback handler(s).";

            _silverbackLogger.LogCallbackHandlerError(new InvalidCredentialException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(InvalidCredentialException),
                expectedMessage,
                1103);
        }

        [Fact]
        public void LogLowLevelTrace_NoException_Logged()
        {
            var expectedMessage = "Message A 42 True";

            _silverbackLogger.LogLowLevelTrace(
                "Message {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
        }

        [Fact]
        public void LogLowLevelTrace_WithException_Logged()
        {
            var expectedMessage = "Message A 42 True";

            _silverbackLogger.LogLowLevelTrace(
                new InvalidComObjectException(),
                "Message {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            _loggerSubstitute.Received(
                LogLevel.Trace,
                typeof(InvalidComObjectException),
                expectedMessage,
                1999);
        }

        [Fact]
        public void LogConsumerLowLevelTrace_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage =
                "Message A 42 True " +
                $"| consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerLowLevelTrace(
                consumer,
                "Message {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
        }

        [Fact]
        public void LogConsumerLowLevelTrace_NoArguments_Logged()
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedMessage = $"Message | consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerLowLevelTrace(consumer, "Message");

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExecuteAndTraceConsumerAction_EnterAndExitLogged(bool mustFail)
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedEnterMessage = $"Enter A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedExitMessage = $"Exit A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var executed = false;

            Action act = () => _silverbackLogger.ExecuteAndTraceConsumerAction(
                consumer,
                () =>
                {
                    _loggerSubstitute.Received(LogLevel.Trace, null, expectedEnterMessage, 1999);
                    executed = true;
                    if (mustFail)
                        throw new InvalidCastException();
                },
                "Enter {string} {int} {bool}",
                "Exit {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            if (mustFail)
                act.Should().Throw<InvalidCastException>();
            else
                act.Should().NotThrow();

            _loggerSubstitute.Received(
                LogLevel.Trace,
                mustFail ? typeof(InvalidCastException) : null,
                expectedExitMessage,
                1999);
            executed.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExecuteAndTraceConsumerAction_EnterAndSuccessOrErrorLogged(bool mustFail)
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedEnterMessage = $"Enter A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedSuccessMessage = $"Success A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedErrorMessage = $"Failure A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var executed = false;

            Action act = () => _silverbackLogger.ExecuteAndTraceConsumerAction(
                consumer,
                () =>
                {
                    _loggerSubstitute.Received(LogLevel.Trace, null, expectedEnterMessage, 1999);
                    executed = true;
                    if (mustFail)
                        throw new InvalidCastException();
                },
                "Enter {string} {int} {bool}",
                "Success {string} {int} {bool}",
                "Failure {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            if (mustFail)
                act.Should().Throw<InvalidCastException>();
            else
                act.Should().NotThrow();

            _loggerSubstitute.Received(
                LogLevel.Trace,
                mustFail ? typeof(InvalidCastException) : null,
                mustFail ? expectedErrorMessage : expectedSuccessMessage,
                1999);

            executed.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAndTraceConsumerActionAsync_EnterAndExitLogged(bool mustFail)
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedEnterMessage = $"Enter A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedExitMessage = $"Exit A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var executed = false;

            Func<Task> act = () => _silverbackLogger.ExecuteAndTraceConsumerActionAsync(
                consumer,
                () =>
                {
                    _loggerSubstitute.Received(LogLevel.Trace, null, expectedEnterMessage, 1999);
                    executed = true;
                    if (mustFail)
                        throw new InvalidCastException();

                    return Task.CompletedTask;
                },
                "Enter {string} {int} {bool}",
                "Exit {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            if (mustFail)
                await act.Should().ThrowAsync<InvalidCastException>();
            else
                await act.Should().NotThrowAsync();

            _loggerSubstitute.Received(
                LogLevel.Trace,
                mustFail ? typeof(InvalidCastException) : null,
                expectedExitMessage,
                1999);

            executed.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAndTraceConsumerActionAsync_EnterAndSuccessOrErrorLogged(bool mustFail)
        {
            var consumer = _serviceProvider.GetRequiredService<TestBroker>()
                .AddConsumer(TestConsumerEndpoint.GetDefault());

            var expectedEnterMessage = $"Enter A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedSuccessMessage = $"Success A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var expectedErrorMessage = $"Failure A 42 True | consumerId: {consumer.Id}, endpointName: test";
            var executed = false;

            Func<Task> act = () => _silverbackLogger.ExecuteAndTraceConsumerActionAsync(
                consumer,
                () =>
                {
                    _loggerSubstitute.Received(LogLevel.Trace, null, expectedEnterMessage, 1999);
                    executed = true;
                    if (mustFail)
                        throw new InvalidCastException();

                    return Task.CompletedTask;
                },
                "Enter {string} {int} {bool}",
                "Success {string} {int} {bool}",
                "Failure {string} {int} {bool}",
                () => new object[] { "A", 42, true });

            if (mustFail)
                await act.Should().ThrowAsync<InvalidCastException>();
            else
                await act.Should().NotThrowAsync();

            _loggerSubstitute.Received(
                LogLevel.Trace,
                mustFail ? typeof(InvalidCastException) : null,
                mustFail ? expectedErrorMessage : expectedSuccessMessage,
                1999);

            executed.Should().BeTrue();
        }
    }
}
