// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaConsumerLocalTimeoutMonitorTests
{
    private readonly PollTimeoutLogger _logger = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnConsumerLog_ShouldNotifyConsumer_WhenPollTimeoutIsLogged(bool enableAutoRecovery)
    {
        using KafkaConsumer consumer = CreateConsumer(enableAutoRecovery);
        KafkaConsumerLocalTimeoutMonitor monitor = new();

        bool result = monitor.OnConsumerLog(new LogMessage("-", SyslogLevel.Warning, "MAXPOLL", "Poll timeout"), consumer);

        result.ShouldBeTrue();
        _logger.Count(KafkaLogEvents.PollTimeoutAutoRecovery.EventId.Name).ShouldBe(enableAutoRecovery ? 1 : 0);
        _logger.Count(KafkaLogEvents.PollTimeoutNoAutoRecovery.EventId.Name).ShouldBe(enableAutoRecovery ? 0 : 1);
    }

    [Fact]
    public void OnConsumerLog_ShouldIgnoreMessage_WhenFacilityIsNotMaxPoll()
    {
        using KafkaConsumer consumer = CreateConsumer(true);
        KafkaConsumerLocalTimeoutMonitor monitor = new();

        bool result = monitor.OnConsumerLog(new LogMessage("-", SyslogLevel.Warning, "FETCH", "Fetch timeout"), consumer);

        result.ShouldBeFalse();
        _logger.Count(KafkaLogEvents.PollTimeoutAutoRecovery.EventId.Name).ShouldBe(0);
        _logger.Count(KafkaLogEvents.PollTimeoutNoAutoRecovery.EventId.Name).ShouldBe(0);
    }

    [Fact]
    public void OnConsumerLog_ShouldIgnoreMessage_WhenLogMessageIsNull()
    {
        using KafkaConsumer consumer = CreateConsumer(true);
        KafkaConsumerLocalTimeoutMonitor monitor = new();

        bool result = monitor.OnConsumerLog(null!, consumer);

        result.ShouldBeFalse();
        _logger.Count(KafkaLogEvents.PollTimeoutAutoRecovery.EventId.Name).ShouldBe(0);
    }

    [Fact]
    public void OnConsumerLog_ShouldIgnoreMessage_WhenConsumerIsNotKafkaConsumer()
    {
        KafkaConsumerLocalTimeoutMonitor monitor = new();

        bool result = monitor.OnConsumerLog(
            new LogMessage("-", SyslogLevel.Warning, "MAXPOLL", "Poll timeout"),
            Substitute.For<IKafkaConsumer>());

        result.ShouldBeFalse();
    }

    private KafkaConsumer CreateConsumer(bool enableAutoRecovery)
    {
        IConfluentConsumerWrapper client = Substitute.For<IConfluentConsumerWrapper>();
        client.Initialized.Returns(new AsyncEvent<BrokerClient>());
        client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());

        return new KafkaConsumer(
            "consumer",
            client,
            new KafkaConsumerConfiguration
            {
                GroupId = "tests",
                EnableAutoRecovery = enableAutoRecovery,
                Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                [
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>([new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            _logger);
    }

    private sealed class PollTimeoutLogger : ISilverbackLogger<KafkaConsumer>, ILogger
    {
        private readonly ConcurrentQueue<string?> _eventNames = new();

        public ILogger InnerLogger => this;

        public int Count(string? eventName) => _eventNames.Count(name => name == eventName);

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _eventNames.Enqueue(eventId.Name);
    }
}
