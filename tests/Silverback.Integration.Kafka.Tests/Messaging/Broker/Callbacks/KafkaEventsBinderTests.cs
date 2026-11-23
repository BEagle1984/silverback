// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Extensions;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Callbacks.Statistics;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Callbacks;

public class KafkaEventsBinderTests
{
    private const string StatisticsJson = """{ "name": "client1" }""";

    private static readonly LogEvent[] ProducerLogEvents =
    [
        KafkaLogEvents.ConfluentProducerLogCritical,
        KafkaLogEvents.ConfluentProducerLogError,
        KafkaLogEvents.ConfluentProducerLogWarning,
        KafkaLogEvents.ConfluentProducerLogInformation,
        KafkaLogEvents.ConfluentProducerLogDebug
    ];

    private static readonly LogEvent[] ConsumerLogEvents =
    [
        KafkaLogEvents.ConfluentConsumerLogCritical,
        KafkaLogEvents.ConfluentConsumerLogError,
        KafkaLogEvents.ConfluentConsumerLogWarning,
        KafkaLogEvents.ConfluentConsumerLogInformation,
        KafkaLogEvents.ConfluentConsumerLogDebug
    ];

    private readonly LoggerSubstitute<KafkaEventsBinderTests> _loggerSubstitute = new(LogLevel.Trace);

    private readonly ISilverbackLogger _logger;

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    public KafkaEventsBinderTests()
    {
        _logger = new SilverbackLogger<KafkaEventsBinderTests>(new MappedLevelsLogger<KafkaEventsBinderTests>([], _loggerSubstitute));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConsumerLogHandler_ShouldLogOnlyIfNoCallbackHandledTheLog(bool firstHandled, bool secondHandled)
    {
        IKafkaConsumerLogCallback firstCallback = Substitute.For<IKafkaConsumerLogCallback>();
        IKafkaConsumerLogCallback secondCallback = Substitute.For<IKafkaConsumerLogCallback>();
        firstCallback.OnConsumerLog(Arg.Any<LogMessage>(), Arg.Any<IKafkaConsumer>()).Returns(firstHandled);
        secondCallback.OnConsumerLog(Arg.Any<LogMessage>(), Arg.Any<IKafkaConsumer>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        Action<IConsumer<byte[]?, byte[]?>, LogMessage>? logHandler = null;
        builder.When(consumerBuilder => consumerBuilder.SetLogHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, LogMessage>>()))
            .Do(call => logHandler = call.Arg<Action<IConsumer<byte[]?, byte[]?>, LogMessage>>());

        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);

        logHandler.ShouldNotBeNull().Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), GetLogMessage());

        firstCallback.ReceivedWithAnyArgs(1).OnConsumerLog(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnConsumerLog(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentConsumerLogWarning);
    }

    [Theory]
    [InlineData(SyslogLevel.Emergency, nameof(KafkaLogEvents.ConfluentConsumerLogCritical))]
    [InlineData(SyslogLevel.Alert, nameof(KafkaLogEvents.ConfluentConsumerLogCritical))]
    [InlineData(SyslogLevel.Critical, nameof(KafkaLogEvents.ConfluentConsumerLogCritical))]
    [InlineData(SyslogLevel.Error, nameof(KafkaLogEvents.ConfluentConsumerLogError))]
    [InlineData(SyslogLevel.Warning, nameof(KafkaLogEvents.ConfluentConsumerLogWarning))]
    [InlineData(SyslogLevel.Notice, nameof(KafkaLogEvents.ConfluentConsumerLogInformation))]
    [InlineData(SyslogLevel.Info, nameof(KafkaLogEvents.ConfluentConsumerLogInformation))]
    [InlineData(SyslogLevel.Debug, nameof(KafkaLogEvents.ConfluentConsumerLogDebug))]
    public void ConsumerLogHandler_ShouldLogWithMappedLevel(SyslogLevel syslogLevel, string expectedLogEventName)
    {
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Action<IConsumer<byte[]?, byte[]?>, LogMessage> logHandler = GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, LogMessage>>(builder);

        logHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), new LogMessage("rdkafka", syslogLevel, "TEST", "test message"));

        AssertLoggedOnly(GetLogEvent(expectedLogEventName), ConsumerLogEvents);
        _loggerSubstitute.Received(
            GetLogEvent(expectedLogEventName).Level,
            null,
            $"{syslogLevel} from Confluent.Kafka consumer: 'test message' | ConsumerName: consumer1");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConsumerErrorHandler_ShouldLogOnlyIfNoCallbackHandledTheError(bool firstHandled, bool secondHandled)
    {
        IKafkaConsumerErrorCallback firstCallback = Substitute.For<IKafkaConsumerErrorCallback>();
        IKafkaConsumerErrorCallback secondCallback = Substitute.For<IKafkaConsumerErrorCallback>();
        firstCallback.OnConsumerError(Arg.Any<Error>(), Arg.Any<IKafkaConsumer>()).Returns(firstHandled);
        secondCallback.OnConsumerError(Arg.Any<Error>(), Arg.Any<IKafkaConsumer>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        Action<IConsumer<byte[]?, byte[]?>, Error>? errorHandler = null;
        builder.When(consumerBuilder => consumerBuilder.SetErrorHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, Error>>()))
            .Do(call => errorHandler = call.Arg<Action<IConsumer<byte[]?, byte[]?>, Error>>());

        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);

        errorHandler.ShouldNotBeNull().Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Transport));

        firstCallback.ReceivedWithAnyArgs(1).OnConsumerError(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnConsumerError(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentConsumerError);
    }

    [Theory]
    [InlineData(ClientStatus.Initialized)]
    [InlineData(ClientStatus.Initializing)]
    public void ConsumerErrorHandler_ShouldLogFatalError_WhenErrorIsFatal(ClientStatus status)
    {
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(GetConsumerWrapper(status: status), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler = GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, Error>>(builder);

        errorHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Fatal, "fatal", true));

        AssertLoggedOnly(KafkaLogEvents.ConfluentConsumerFatalError, KafkaLogEvents.ConfluentConsumerError);
        _loggerSubstitute.Received(
            KafkaLogEvents.ConfluentConsumerFatalError.Level,
            null,
            $"Fatal error in Kafka consumer: 'fatal' ({(int)ErrorCode.Local_Fatal}) | ConsumerName: consumer1");
    }

    [Theory]
    [InlineData(ClientStatus.Disconnected)]
    [InlineData(ClientStatus.Disconnecting)]
    public void ConsumerErrorHandler_ShouldIgnoreError_WhenConsumerNotInitialized(ClientStatus status)
    {
        IKafkaConsumerErrorCallback callback = Substitute.For<IKafkaConsumerErrorCallback>();
        SetupCallbacks(callback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(GetConsumerWrapper(status: status), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler = GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, Error>>(builder);

        errorHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Fatal, "fatal", true));

        callback.DidNotReceive().OnConsumerError(Arg.Any<Error>(), Arg.Any<IKafkaConsumer>());
        AssertNotLogged(KafkaLogEvents.ConfluentConsumerFatalError, KafkaLogEvents.ConfluentConsumerError);
    }

    [Fact]
    public void ConsumerStatisticsHandler_ShouldLogAndInvokeCallbacks()
    {
        IKafkaConsumerStatisticsCallback callback = Substitute.For<IKafkaConsumerStatisticsCallback>();
        SetupCallbacks(callback);

        IConfluentConsumerWrapper consumerWrapper = GetConsumerWrapper();
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(consumerWrapper, new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler = GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, string>>(builder);

        statisticsHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), StatisticsJson);

        callback.Received(1).OnConsumerStatistics(
            Arg.Is<KafkaStatistics?>(statistics => statistics != null && statistics.Name == "client1"),
            StatisticsJson,
            consumerWrapper.Consumer);
        _callbacksInvoker.Received(1).Invoke(Arg.Any<Action<IKafkaConsumerStatisticsCallback>>(), Arg.Any<IServiceProvider?>(), false);
        _loggerSubstitute.Received(
            KafkaLogEvents.ConsumerStatisticsReceived.Level,
            null,
            $"Kafka consumer statistics received: {StatisticsJson} | ConsumerName: consumer1",
            KafkaLogEvents.ConsumerStatisticsReceived.EventId.Id);
    }

    [Fact]
    public void PartitionsAssignedHandler_ShouldLogAndAssignWithUnsetOffsets_WhenNoCallbackReturnsOffsets()
    {
        IKafkaPartitionsAssignedCallback callback = Substitute.For<IKafkaPartitionsAssignedCallback>();
        callback.OnPartitionsAssigned(Arg.Any<IReadOnlyCollection<TopicPartition>>(), Arg.Any<IKafkaConsumer>())
            .Returns((IEnumerable<TopicPartitionOffset>?)null);
        SetupCallbacks(callback);

        IConfluentConsumerWrapper consumerWrapper = GetConsumerWrapper();
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(consumerWrapper, new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> assignedHandler =
            GetRegisteredHandler<Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>>(builder);
        List<TopicPartition> topicPartitions = [new("topic1", 0), new("topic1", 1)];

        IEnumerable<TopicPartitionOffset> result = assignedHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), topicPartitions);

        result.ShouldBe(
        [
            new TopicPartitionOffset("topic1", 0, Offset.Unset),
            new TopicPartitionOffset("topic1", 1, Offset.Unset)
        ]);
        callback.Received(1).OnPartitionsAssigned(topicPartitions, consumerWrapper.Consumer);
        _loggerSubstitute.Received(LogLevel.Information, null, "Assigned partition topic1[0] | ConsumerName: consumer1");
        _loggerSubstitute.Received(LogLevel.Information, null, "Assigned partition topic1[1] | ConsumerName: consumer1");
        AssertNotLogged(KafkaLogEvents.PartitionOffsetReset);
    }

    [Fact]
    public void PartitionsAssignedHandler_ShouldAssignAndLogOffsetsReturnedByCallback()
    {
        IKafkaPartitionsAssignedCallback callback = Substitute.For<IKafkaPartitionsAssignedCallback>();
        callback.OnPartitionsAssigned(Arg.Any<IReadOnlyCollection<TopicPartition>>(), Arg.Any<IKafkaConsumer>())
            .Returns(
            [
                new TopicPartitionOffset("topic1", 0, 42),
                new TopicPartitionOffset("topic1", 1, Offset.Unset)
            ]);
        SetupCallbacks(callback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> assignedHandler =
            GetRegisteredHandler<Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>>(builder);

        IEnumerable<TopicPartitionOffset> result = assignedHandler.Invoke(
            Substitute.For<IConsumer<byte[]?, byte[]?>>(),
            [new TopicPartition("topic1", 0), new TopicPartition("topic1", 1)]);

        result.ShouldBe(
        [
            new TopicPartitionOffset("topic1", 0, 42),
            new TopicPartitionOffset("topic1", 1, Offset.Unset)
        ]);
        _loggerSubstitute.Received(
            KafkaLogEvents.PartitionOffsetReset.Level,
            null,
            "topic1[0] offset will be reset to 42 | ConsumerName: consumer1",
            KafkaLogEvents.PartitionOffsetReset.EventId.Id);
        _loggerSubstitute.DidNotReceive(
            KafkaLogEvents.PartitionOffsetReset.Level,
            null,
            $"topic1[1] offset will be reset to {Offset.Unset.Value} | ConsumerName: consumer1",
            KafkaLogEvents.PartitionOffsetReset.EventId.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(PartitionAssignmentStrategy.Range)]
    [InlineData(PartitionAssignmentStrategy.CooperativeSticky)]
    public void PartitionsRevokedHandler_ShouldRevokeFromConsumerAndLogAndInvokeCallbacks(PartitionAssignmentStrategy? strategy)
    {
        IKafkaPartitionsRevokedCallback callback = Substitute.For<IKafkaPartitionsRevokedCallback>();
        SetupCallbacks(callback);

        IConfluentConsumerWrapper consumerWrapper = GetConsumerWrapper(new KafkaConsumerConfiguration { EnableAutoCommit = false });
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(
            consumerWrapper,
            new KafkaConsumerConfiguration { PartitionAssignmentStrategy = strategy },
            _callbacksInvoker,
            _logger);
        Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>> revokedHandler =
            GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>>>(builder);
        List<TopicPartitionOffset> topicPartitionOffsets =
        [
            new("topic1", 0, 42),
            new("topic1", 1, 13)
        ];

        revokedHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), topicPartitionOffsets);

        consumerWrapper.Received(1).Commit();
        callback.Received(1).OnPartitionsRevoked(topicPartitionOffsets, consumerWrapper.Consumer);
        _loggerSubstitute.Received(LogLevel.Information, null, "Revoked partition topic1[0]@42 | ConsumerName: consumer1");
        _loggerSubstitute.Received(LogLevel.Information, null, "Revoked partition topic1[1]@13 | ConsumerName: consumer1");
    }

    [Fact]
    public void OffsetsCommittedHandler_ShouldLogCommittedOffsetsAndErrorsAndInvokeCallbacks()
    {
        IKafkaOffsetCommittedCallback callback = Substitute.For<IKafkaOffsetCommittedCallback>();
        SetupCallbacks(callback);

        IConfluentConsumerWrapper consumerWrapper = GetConsumerWrapper();
        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        builder.SetEventsHandlers(consumerWrapper, new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);
        Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> committedHandler =
            GetRegisteredHandler<Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>>(builder);
        CommittedOffsets committedOffsets = new(
            [
                new TopicPartitionOffsetError("topic1", 0, Offset.Unset, new Error(ErrorCode.NoError)),
                new TopicPartitionOffsetError("topic1", 1, 10, null),
                new TopicPartitionOffsetError("topic1", 2, 20, new Error(ErrorCode.NoError)),
                new TopicPartitionOffsetError("topic1", 3, 30, new Error(ErrorCode.Local_Transport, "test error"))
            ],
            new Error(ErrorCode.NoError));

        committedHandler.Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), committedOffsets);

        callback.Received(1).OnOffsetsCommitted(committedOffsets, consumerWrapper.Consumer);
        _loggerSubstitute.DidNotReceive(
            KafkaLogEvents.OffsetCommitted.Level,
            null,
            $"Successfully committed offset topic1[0]@{Offset.Unset.Value} | ConsumerName: consumer1");
        _loggerSubstitute.Received(
            KafkaLogEvents.OffsetCommitted.Level,
            null,
            "Successfully committed offset topic1[1]@10 | ConsumerName: consumer1",
            KafkaLogEvents.OffsetCommitted.EventId.Id);
        _loggerSubstitute.Received(
            KafkaLogEvents.OffsetCommitted.Level,
            null,
            "Successfully committed offset topic1[2]@20 | ConsumerName: consumer1",
            KafkaLogEvents.OffsetCommitted.EventId.Id);
        _loggerSubstitute.Received(
            KafkaLogEvents.OffsetCommitError.Level,
            null,
            $"Error occurred committing offset topic1[3]@30: 'test error' ({(int)ErrorCode.Local_Transport}) | ConsumerName: consumer1",
            KafkaLogEvents.OffsetCommitError.EventId.Id);
        _loggerSubstitute.DidNotReceive(
            KafkaLogEvents.OffsetCommitted.Level,
            null,
            "Successfully committed offset topic1[3]@30 | ConsumerName: consumer1");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ProducerLogHandler_ShouldLogOnlyIfNoCallbackHandledTheLog(bool firstHandled, bool secondHandled)
    {
        IKafkaProducerLogCallback firstCallback = Substitute.For<IKafkaProducerLogCallback>();
        IKafkaProducerLogCallback secondCallback = Substitute.For<IKafkaProducerLogCallback>();
        firstCallback.OnProducerLog(Arg.Any<LogMessage>(), Arg.Any<IConfluentProducerWrapper>()).Returns(firstHandled);
        secondCallback.OnProducerLog(Arg.Any<LogMessage>(), Arg.Any<IConfluentProducerWrapper>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentProducerBuilder builder = GetProducerBuilder();
        Action<IProducer<byte[]?, byte[]?>, LogMessage>? logHandler = null;
        builder.When(producerBuilder => producerBuilder.SetLogHandler(Arg.Any<Action<IProducer<byte[]?, byte[]?>, LogMessage>>()))
            .Do(call => logHandler = call.Arg<Action<IProducer<byte[]?, byte[]?>, LogMessage>>());

        builder.SetEventsHandlers(Substitute.For<IConfluentProducerWrapper>(), _callbacksInvoker, _logger);

        logHandler.ShouldNotBeNull().Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), GetLogMessage());

        firstCallback.ReceivedWithAnyArgs(1).OnProducerLog(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnProducerLog(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentProducerLogWarning);
    }

    [Theory]
    [InlineData(SyslogLevel.Emergency, nameof(KafkaLogEvents.ConfluentProducerLogCritical))]
    [InlineData(SyslogLevel.Alert, nameof(KafkaLogEvents.ConfluentProducerLogCritical))]
    [InlineData(SyslogLevel.Critical, nameof(KafkaLogEvents.ConfluentProducerLogCritical))]
    [InlineData(SyslogLevel.Error, nameof(KafkaLogEvents.ConfluentProducerLogError))]
    [InlineData(SyslogLevel.Warning, nameof(KafkaLogEvents.ConfluentProducerLogWarning))]
    [InlineData(SyslogLevel.Notice, nameof(KafkaLogEvents.ConfluentProducerLogInformation))]
    [InlineData(SyslogLevel.Info, nameof(KafkaLogEvents.ConfluentProducerLogInformation))]
    [InlineData(SyslogLevel.Debug, nameof(KafkaLogEvents.ConfluentProducerLogDebug))]
    public void ProducerLogHandler_ShouldLogWithMappedLevel(SyslogLevel syslogLevel, string expectedLogEventName)
    {
        IConfluentProducerBuilder builder = GetProducerBuilder();
        builder.SetEventsHandlers(GetProducerWrapper(), _callbacksInvoker, _logger);
        Action<IProducer<byte[]?, byte[]?>, LogMessage> logHandler = GetRegisteredHandler<Action<IProducer<byte[]?, byte[]?>, LogMessage>>(builder);

        logHandler.Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), new LogMessage("rdkafka", syslogLevel, "TEST", "test message"));

        AssertLoggedOnly(GetLogEvent(expectedLogEventName), ProducerLogEvents);
        _loggerSubstitute.Received(
            GetLogEvent(expectedLogEventName).Level,
            null,
            $"{syslogLevel} from Confluent.Kafka producer: 'test message' | ProducerName: producer1");
    }

    [Theory]
    [InlineData(ClientStatus.Initialized, false, nameof(KafkaLogEvents.ConfluentProducerError))]
    [InlineData(ClientStatus.Initializing, false, nameof(KafkaLogEvents.ConfluentProducerError))]
    [InlineData(ClientStatus.Initialized, true, nameof(KafkaLogEvents.ConfluentProducerFatalError))]
    [InlineData(ClientStatus.Initializing, true, nameof(KafkaLogEvents.ConfluentProducerFatalError))]
    public void ProducerErrorHandler_ShouldLogError(ClientStatus status, bool isFatal, string expectedLogEventName)
    {
        IConfluentProducerBuilder builder = GetProducerBuilder();
        builder.SetEventsHandlers(GetProducerWrapper(status), _callbacksInvoker, _logger);
        Action<IProducer<byte[]?, byte[]?>, Error> errorHandler = GetRegisteredHandler<Action<IProducer<byte[]?, byte[]?>, Error>>(builder);

        errorHandler.Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Transport, "test error", isFatal));

        AssertLoggedOnly(
            GetLogEvent(expectedLogEventName),
            KafkaLogEvents.ConfluentProducerError,
            KafkaLogEvents.ConfluentProducerFatalError);
        _loggerSubstitute.Received(
            GetLogEvent(expectedLogEventName).Level,
            null,
            $"{(isFatal ? "Fatal error" : "Error")} in Kafka producer: 'test error' ({(int)ErrorCode.Local_Transport}) | ProducerName: producer1");
    }

    [Theory]
    [InlineData(ClientStatus.Disconnected)]
    [InlineData(ClientStatus.Disconnecting)]
    public void ProducerErrorHandler_ShouldIgnoreError_WhenProducerNotInitialized(ClientStatus status)
    {
        IConfluentProducerBuilder builder = GetProducerBuilder();
        builder.SetEventsHandlers(GetProducerWrapper(status), _callbacksInvoker, _logger);
        Action<IProducer<byte[]?, byte[]?>, Error> errorHandler = GetRegisteredHandler<Action<IProducer<byte[]?, byte[]?>, Error>>(builder);

        errorHandler.Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Fatal, "fatal", true));

        AssertNotLogged(KafkaLogEvents.ConfluentProducerError, KafkaLogEvents.ConfluentProducerFatalError);
    }

    [Fact]
    public void ProducerStatisticsHandler_ShouldLogAndInvokeCallbacks()
    {
        IKafkaProducerStatisticsCallback callback = Substitute.For<IKafkaProducerStatisticsCallback>();
        SetupCallbacks(callback);

        IConfluentProducerWrapper producerWrapper = GetProducerWrapper();
        IConfluentProducerBuilder builder = GetProducerBuilder();
        builder.SetEventsHandlers(producerWrapper, _callbacksInvoker, _logger);
        Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler = GetRegisteredHandler<Action<IProducer<byte[]?, byte[]?>, string>>(builder);

        statisticsHandler.Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), StatisticsJson);

        callback.Received(1).OnProducerStatistics(
            Arg.Is<KafkaStatistics?>(statistics => statistics != null && statistics.Name == "client1"),
            StatisticsJson,
            producerWrapper);
        _callbacksInvoker.Received(1).Invoke(Arg.Any<Action<IKafkaProducerStatisticsCallback>>(), Arg.Any<IServiceProvider?>(), false);
        _loggerSubstitute.Received(
            KafkaLogEvents.ProducerStatisticsReceived.Level,
            null,
            $"Kafka producer statistics received: {StatisticsJson} | ProducerName: producer1",
            KafkaLogEvents.ProducerStatisticsReceived.EventId.Id);
    }

    [Fact]
    public void ProducerStatisticsHandler_ShouldPassNullStatistics_WhenJsonNotValid()
    {
        IKafkaProducerStatisticsCallback callback = Substitute.For<IKafkaProducerStatisticsCallback>();
        SetupCallbacks(callback);

        IConfluentProducerWrapper producerWrapper = GetProducerWrapper();
        IConfluentProducerBuilder builder = GetProducerBuilder();
        builder.SetEventsHandlers(producerWrapper, _callbacksInvoker, _logger);
        Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler = GetRegisteredHandler<Action<IProducer<byte[]?, byte[]?>, string>>(builder);

        statisticsHandler.Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), "{ WTF?! }");

        callback.Received(1).OnProducerStatistics(null, "{ WTF?! }", producerWrapper);
        _loggerSubstitute.Received(
            KafkaLogEvents.StatisticsDeserializationError.Level,
            typeof(System.Text.Json.JsonException),
            eventId: KafkaLogEvents.StatisticsDeserializationError.EventId.Id);
    }

    private static IConfluentConsumerBuilder GetConsumerBuilder()
    {
        IConfluentConsumerBuilder builder = Substitute.For<IConfluentConsumerBuilder>();
        builder.ReturnsForAll(builder);

        return builder;
    }

    private static IConfluentProducerBuilder GetProducerBuilder()
    {
        IConfluentProducerBuilder builder = Substitute.For<IConfluentProducerBuilder>();
        builder.ReturnsForAll(builder);

        return builder;
    }

    private static THandler GetRegisteredHandler<THandler>(object builder)
        where THandler : Delegate =>
        builder.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<THandler>().ShouldHaveSingleItem();

    private static IConfluentConsumerWrapper GetConsumerWrapper(
        KafkaConsumerConfiguration? consumerConfiguration = null,
        ClientStatus status = ClientStatus.Initialized)
    {
        IConfluentConsumerWrapper consumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        consumerWrapper.Status.Returns(status);
        consumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        consumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());

        KafkaConsumer consumer = new(
            "consumer1",
            consumerWrapper,
            consumerConfiguration ?? new KafkaConsumerConfiguration(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
        consumerWrapper.Consumer.Returns(consumer);

        return consumerWrapper;
    }

    private static IConfluentProducerWrapper GetProducerWrapper(ClientStatus status = ClientStatus.Initialized)
    {
        IConfluentProducerWrapper producerWrapper = Substitute.For<IConfluentProducerWrapper>();
        producerWrapper.Status.Returns(status);
        producerWrapper.DisplayName.Returns("producer1");

        KafkaProducer producer = new(
            "producer1",
            producerWrapper,
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());
        producerWrapper.Producer.Returns(producer);

        return producerWrapper;
    }

    private static LogMessage GetLogMessage() => new("rdkafka", SyslogLevel.Warning, "TEST", "test message");

    private static LogEvent GetLogEvent(string name) =>
        typeof(KafkaLogEvents).GetProperty(name).ShouldNotBeNull().GetValue(null).ShouldBeOfType<LogEvent>();

    private void SetupCallbacks<TCallback>(params TCallback[] callbacks) =>
        _callbacksInvoker
            .When(invoker => invoker.Invoke(Arg.Any<Action<TCallback>>(), Arg.Any<IServiceProvider?>(), Arg.Any<bool>()))
            .Do(call =>
            {
                foreach (TCallback callback in callbacks)
                {
                    call.Arg<Action<TCallback>>().Invoke(callback);
                }
            });

    private void AssertLogged(bool handled, LogEvent logEvent)
    {
        if (handled)
            _loggerSubstitute.DidNotReceive(logEvent.Level, null, eventId: logEvent.EventId.Id);
        else
            _loggerSubstitute.Received(logEvent.Level, null, eventId: logEvent.EventId.Id);
    }

    private void AssertLoggedOnly(LogEvent expectedLogEvent, params LogEvent[] logEvents)
    {
        _loggerSubstitute.Received(expectedLogEvent.Level, null, eventId: expectedLogEvent.EventId.Id);

        AssertNotLogged([.. logEvents.Where(logEvent => logEvent.EventId.Id != expectedLogEvent.EventId.Id)]);
    }

    private void AssertNotLogged(params LogEvent[] logEvents)
    {
        foreach (LogEvent logEvent in logEvents)
        {
            _loggerSubstitute.DidNotReceive(logEvent.Level, null, eventId: logEvent.EventId.Id);
        }
    }
}
