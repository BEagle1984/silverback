// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Integration.Kafka.TestTypes;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentAdminClientFactoryTests
{
    private readonly ProducerCollection _producers = [];

    private readonly ConsumerCollection _consumers = [];

    // Builds no librdkafka client, so that the tests of the resolution logic are fast. The tests that need the mapped configuration to
    // be accepted by librdkafka build real clients with GetRealFactory, against a loopback server, because a client whose broker host
    // has to be resolved by DNS takes seconds to be disposed.
    private readonly FakeAdminClientFactory _factory;

    public ConfluentAdminClientFactoryTests()
    {
        _factory = new FakeAdminClientFactory(_producers, _consumers);
    }

    [Fact]
    public async Task GetClient_ShouldReturnClientForBuilderAction()
    {
        await using DisconnectingLoopbackServer server = new();
        string bootstrapServers = server.BootstrapServers;

        using IAdminClient client = GetRealFactory().GetClient(builder => builder.WithBootstrapServers(bootstrapServers));

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetClient_ShouldNameClientAfterClientId()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new AdminClientConfig
            {
                BootstrapServers = server.BootstrapServers,
                ClientId = "client1"
            });

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public async Task GetClient_WithConfiguration_ShouldReturnClientForClientConfiguration()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new KafkaClientConfiguration
            {
                BootstrapServers = server.BootstrapServers
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetClient_WithConfiguration_ShouldReturnClientForProducerConfiguration()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new KafkaProducerConfiguration
            {
                BootstrapServers = server.BootstrapServers
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetClient_WithConfiguration_ShouldIgnoreProducerSpecificConfigurations()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new KafkaProducerConfiguration
            {
                BootstrapServers = server.BootstrapServers,
                EnableDeliveryReports = false
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetClient_WithConfiguration_ShouldReturnClientForConsumerConfiguration()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = server.BootstrapServers
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetClient_WithConfiguration_ShouldIgnoreConsumerSpecificConfigurations()
    {
        await using DisconnectingLoopbackServer server = new();

        using IAdminClient client = GetRealFactory().GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = server.BootstrapServers,
                ConsumeResultFields = "something"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaProducer()
    {
        KafkaProducer producer = new(
            "producer1",
            Substitute.For<IConfluentProducerWrapper>(),
            GetProducerConfiguration("client1", new KafkaStaticProducerEndpointResolver("topic1")),
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

        using IAdminClient client = _factory.GetClient(producer);

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaTransactionalProducer()
    {
        KafkaTransactionalProducer producer = GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1"));

        using IAdminClient client = _factory.GetClient(producer);

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldThrow_WhenNotKafkaProducer()
    {
        Action act = () => _factory.GetClient(Substitute.For<IProducer>());

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The producer is not a Kafka producer.");
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaConsumer()
    {
        using IAdminClient client = _factory.GetClient(GetConsumer("client1"));

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldThrow_WhenNotKafkaConsumer()
    {
        Action act = () => _factory.GetClient(Substitute.For<IConsumer>());

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The consumer is not a Kafka consumer.");
    }

    [Fact]
    public void GetClientForProducer_ShouldReturnClientForKafkaProducer()
    {
        SetProducers(GetKafkaProducer("producer", "client1"));

        using IAdminClient client = _factory.GetClientForProducer("producer");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducer_ShouldNotMatchGeneratedName()
    {
        SetProducers(GetKafkaProducer("orders", "client1"), GetKafkaProducer("orders-1", "client2"));

        using IAdminClient client = _factory.GetClientForProducer("orders-1");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducer_ShouldReturnClientForKafkaTransactionalProducer()
    {
        SetProducers(
            new KafkaTransactionalProducer(
                "producer",
                GetProducerConfiguration("client1", new KafkaStaticProducerEndpointResolver("topic1")),
                Substitute.For<IKafkaTransactionalProducerCollection>()));

        using IAdminClient client = _factory.GetClientForProducer("producer");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducer_ShouldIgnoreOtherBrokersProducers()
    {
        IProducer otherBrokerProducer = Substitute.For<IProducer>();
        otherBrokerProducer.Name.Returns("producer");
        otherBrokerProducer.EndpointConfiguration.Returns(
            new KafkaProducerEndpointConfiguration
            {
                EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
            });
        SetProducers(otherBrokerProducer);

        Action act = () => _factory.GetClientForProducer("producer");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka producer has been configured with name 'producer'.");
    }

    [Fact]
    public void GetClientForProducer_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => _factory.GetClientForProducer(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void GetClientForConsumer_ShouldReturnClientForMatchingConsumer()
    {
        SetConsumers(GetConsumer("client1"), GetConsumer("client2"));

        using IAdminClient client = _factory.GetClientForConsumer("client2");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumer_ShouldThrow_WhenConsumerNotFound()
    {
        SetConsumers(GetConsumer("client1"));

        Action act = () => _factory.GetClientForConsumer("client2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka consumer has been configured with name 'client2'.");
    }

    [Fact]
    public void GetClientForConsumer_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => _factory.GetClientForConsumer(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData("friendly")]
    [InlineData("topic2")]
    [InlineData("topic2[1]")]
    public void GetClientForProducerEndpoint_ShouldReturnClientForMatchingProducer(string endpointName)
    {
        SetProducers(
            GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1")),
            GetProducer("client2", new KafkaStaticProducerEndpointResolver("topic2", 1), "friendly"));

        using IAdminClient client = _factory.GetClientForProducerEndpoint(endpointName);

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldPreferFriendlyName_WhenMatchingBothFriendlyAndTopicName()
    {
        SetProducers(
            GetProducer("client1", new KafkaStaticProducerEndpointResolver("name")),
            GetProducer("client2", new KafkaStaticProducerEndpointResolver("topic"), "name"));

        using IAdminClient client = _factory.GetClientForProducerEndpoint("name");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldIgnoreOtherBrokersProducers()
    {
        IProducer otherBrokerProducer = Substitute.For<IProducer>();
        otherBrokerProducer.EndpointConfiguration.Returns(
            new KafkaProducerEndpointConfiguration
            {
                EndpointResolver = new KafkaStaticProducerEndpointResolver("topic")
            });
        SetProducers(otherBrokerProducer, GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic")));

        using IAdminClient client = _factory.GetClientForProducerEndpoint("topic");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldThrow_WhenOnlyDynamicEndpointResolvesToTopic()
    {
        SetProducers(GetProducer("client1", new KafkaDynamicProducerEndpointResolver<object>(_ => "topic")));

        Action act = () => _factory.GetClientForProducerEndpoint("topic");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka producer has been configured for endpoint 'topic'.");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldThrow_WhenEndpointNotFound()
    {
        SetProducers(GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1")));

        Action act = () => _factory.GetClientForProducerEndpoint("topic2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka producer has been configured for endpoint 'topic2'.");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldThrow_WhenEndpointNameIsNull()
    {
        Action act = () => _factory.GetClientForProducerEndpoint(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData("friendly")]
    [InlineData("topic2")]
    [InlineData("topic3")]
    [InlineData("topic2,topic3[1]")]
    public void GetClientForConsumerEndpoint_ShouldReturnClientForMatchingConsumer(string endpointName)
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("topic1")),
            GetConsumer(
                "client2",
                GetConsumerEndpoint("other"),
                GetConsumerEndpoint(
                    "friendly",
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic3", 1, Offset.Unset))));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint(endpointName);

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldPreferFriendlyName_WhenMatchingBothFriendlyAndTopicName()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("name")),
            GetConsumer(
                "client2",
                GetConsumerEndpoint("name", new TopicPartitionOffset("topic", Partition.Any, Offset.Unset))));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("name");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldReturnClientForConsumerSubscribingMatchingPattern()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("topic1")),
            GetConsumer("client2", GetConsumerEndpoint("^orders-[a-z]+$")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldRequirePatternToMatchEntireName_WhenUsingConsumerProtocol()
    {
        IKafkaConsumer prefixConsumer = GetConsumer("client1", GetConsumerEndpoint("^orders"));
        prefixConsumer.Configuration.Returns(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ClientId = "client1",
                GroupProtocol = GroupProtocol.Consumer
            });
        SetConsumers(prefixConsumer, GetConsumer("client2", GetConsumerEndpoint("^orders-.*")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNameIsPatternMatchingItself()
    {
        SetConsumers(GetConsumer("client1", GetConsumerEndpoint("other", new TopicPartitionOffset("^.*", Partition.Any, Offset.Unset))));

        Action act = () => _factory.GetClientForConsumerEndpoint("^.+");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldPreferTopicName_WhenMatchingBothTopicNameAndPattern()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("^orders-")),
            GetConsumer("client2", GetConsumerEndpoint("orders-eu")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldIgnoreOtherBrokersConsumers()
    {
        IConsumer otherBrokerConsumer = Substitute.For<IConsumer>();
        otherBrokerConsumer.EndpointsConfiguration.Returns([GetConsumerEndpoint("topic")]);
        SetConsumers(otherBrokerConsumer, GetConsumer("client1", GetConsumerEndpoint("topic")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("topic");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNotFound()
    {
        SetConsumers(GetConsumer("client1", GetConsumerEndpoint("topic1")));

        Action act = () => _factory.GetClientForConsumerEndpoint("topic2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka consumer has been configured for endpoint 'topic2'.");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNameIsNull()
    {
        Action act = () => _factory.GetClientForConsumerEndpoint(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public async Task GetClient_ShouldSetLogHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        RecordingLogger logger = new();
        ConfluentAdminClientFactory factory = new(_producers, _consumers, GetSilverbackLogger(logger));

        using IAdminClient client = factory.GetClient(
            new AdminClientConfig
            {
                BootstrapServers = server.BootstrapServers,
                Debug = "generic"
            });

        RecordedLog log = await logger.WaitForLogAsync(2315);
        log.LogLevel.ShouldBe(LogLevel.Debug);
        log.Message.ShouldStartWith("Debug from Confluent.Kafka admin client: '");
    }

    [Fact]
    public async Task GetClient_ShouldSetErrorHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        RecordingLogger logger = new();
        ConfluentAdminClientFactory factory = new(_producers, _consumers, GetSilverbackLogger(logger));

        using IAdminClient client = factory.GetClient(new AdminClientConfig { BootstrapServers = server.BootstrapServers });

        // The server closing every connection causes non-fatal errors
        RecordedLog log = await logger.WaitForLogAsync(2301);
        log.LogLevel.ShouldBe(LogLevel.Warning);
        log.Message.ShouldStartWith("Error in Kafka admin client: '");
    }

    [Theory]
    [InlineData(false, nameof(KafkaLogEvents.ConfluentAdminClientError))]
    [InlineData(true, nameof(KafkaLogEvents.ConfluentAdminClientFatalError))]
    public void OnError_ShouldLogError(bool isFatal, string expectedLogEventName)
    {
        LoggerSubstitute<ConfluentAdminClientFactory> loggerSubstitute = new(LogLevel.Trace);
        LogEvent expectedLogEvent = GetLogEvent(expectedLogEventName);

        ConfluentAdminClientFactory.OnError(new Error(ErrorCode.Local_Transport, "test error", isFatal), GetMappedLevelsLogger(loggerSubstitute));

        loggerSubstitute.Received(
            expectedLogEvent.Level,
            null,
            $"{(isFatal ? "Fatal error" : "Error")} in Kafka admin client: 'test error' ({(int)ErrorCode.Local_Transport})",
            expectedLogEvent.EventId.Id);
        AssertNotLogged(loggerSubstitute, expectedLogEvent, KafkaLogEvents.ConfluentAdminClientError, KafkaLogEvents.ConfluentAdminClientFatalError);
    }

    [Theory]
    [InlineData(SyslogLevel.Emergency, nameof(KafkaLogEvents.ConfluentAdminClientLogCritical))]
    [InlineData(SyslogLevel.Alert, nameof(KafkaLogEvents.ConfluentAdminClientLogCritical))]
    [InlineData(SyslogLevel.Critical, nameof(KafkaLogEvents.ConfluentAdminClientLogCritical))]
    [InlineData(SyslogLevel.Error, nameof(KafkaLogEvents.ConfluentAdminClientLogError))]
    [InlineData(SyslogLevel.Warning, nameof(KafkaLogEvents.ConfluentAdminClientLogWarning))]
    [InlineData(SyslogLevel.Notice, nameof(KafkaLogEvents.ConfluentAdminClientLogInformation))]
    [InlineData(SyslogLevel.Info, nameof(KafkaLogEvents.ConfluentAdminClientLogInformation))]
    [InlineData(SyslogLevel.Debug, nameof(KafkaLogEvents.ConfluentAdminClientLogDebug))]
    public void OnLog_ShouldLogWithMappedLevel(SyslogLevel syslogLevel, string expectedLogEventName)
    {
        LoggerSubstitute<ConfluentAdminClientFactory> loggerSubstitute = new(LogLevel.Trace);
        LogEvent expectedLogEvent = GetLogEvent(expectedLogEventName);

        ConfluentAdminClientFactory.OnLog(new LogMessage("rdkafka", syslogLevel, "TEST", "test message"), GetMappedLevelsLogger(loggerSubstitute));

        loggerSubstitute.Received(
            expectedLogEvent.Level,
            null,
            $"{syslogLevel} from Confluent.Kafka admin client: 'test message'",
            expectedLogEvent.EventId.Id);
        AssertNotLogged(
            loggerSubstitute,
            expectedLogEvent,
            KafkaLogEvents.ConfluentAdminClientLogCritical,
            KafkaLogEvents.ConfluentAdminClientLogError,
            KafkaLogEvents.ConfluentAdminClientLogWarning,
            KafkaLogEvents.ConfluentAdminClientLogInformation,
            KafkaLogEvents.ConfluentAdminClientLogDebug);
    }

    private static SilverbackLogger<ConfluentAdminClientFactory> GetMappedLevelsLogger(LoggerSubstitute<ConfluentAdminClientFactory> loggerSubstitute) =>
        new(new MappedLevelsLogger<ConfluentAdminClientFactory>([], loggerSubstitute));

    private static LogEvent GetLogEvent(string name) =>
        typeof(KafkaLogEvents).GetProperty(name).ShouldNotBeNull().GetValue(null).ShouldBeOfType<LogEvent>();

    private static void AssertNotLogged(LoggerSubstitute<ConfluentAdminClientFactory> loggerSubstitute, LogEvent expectedLogEvent, params LogEvent[] logEvents)
    {
        foreach (LogEvent logEvent in logEvents.Where(logEvent => logEvent.EventId.Id != expectedLogEvent.EventId.Id))
        {
            loggerSubstitute.DidNotReceive(logEvent.Level, null, eventId: logEvent.EventId.Id);
        }
    }

    private static ISilverbackLogger<ConfluentAdminClientFactory> GetSilverbackLogger(ILogger innerLogger)
    {
        ISilverbackLogger<ConfluentAdminClientFactory> logger = Substitute.For<ISilverbackLogger<ConfluentAdminClientFactory>>();
        logger.InnerLogger.Returns(innerLogger);
        logger.IsEnabled(Arg.Any<LogEvent>()).Returns(true);
        return logger;
    }

    private static KafkaTransactionalProducer GetProducer(
        string clientId,
        IProducerEndpointResolver<KafkaProducerEndpoint> endpointResolver,
        string? friendlyName = null) =>
        new(
            "producer",
            GetProducerConfiguration(clientId, endpointResolver, friendlyName),
            Substitute.For<IKafkaTransactionalProducerCollection>());

    private static KafkaProducer GetKafkaProducer(string addedName, string clientId)
    {
        IConfluentProducerWrapper wrapper = Substitute.For<IConfluentProducerWrapper>();
        wrapper.Name.Returns(addedName);

        return new KafkaProducer(
            $"{addedName}-1",
            wrapper,
            GetProducerConfiguration(clientId, new KafkaStaticProducerEndpointResolver($"topic-{clientId}")),
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());
    }

    private static KafkaProducerConfiguration GetProducerConfiguration(
        string clientId,
        IProducerEndpointResolver<KafkaProducerEndpoint> endpointResolver,
        string? friendlyName = null) =>
        new()
        {
            BootstrapServers = "PLAINTEXT://test:9092",
            ClientId = clientId,
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
            [
                new KafkaProducerEndpointConfiguration
                {
                    EndpointResolver = endpointResolver,
                    FriendlyName = friendlyName
                }
            ])
        };

    private static IKafkaConsumer GetConsumer(string clientId, params KafkaConsumerEndpointConfiguration[] endpoints)
    {
        IKafkaConsumer consumer = Substitute.For<IKafkaConsumer>();
        consumer.Name.Returns(clientId);
        consumer.Configuration.Returns(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ClientId = clientId
            });
        consumer.EndpointsConfiguration.Returns(endpoints);

        return consumer;
    }

    private static KafkaConsumerEndpointConfiguration GetConsumerEndpoint(string topic) =>
        new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset(topic, Partition.Any, Offset.Unset)
            ])
        };

    private static KafkaConsumerEndpointConfiguration GetConsumerEndpoint(string friendlyName, params TopicPartitionOffset[] topicPartitions) =>
        new()
        {
            FriendlyName = friendlyName,
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(topicPartitions)
        };

    private void SetProducers(params IProducer[] producers)
    {
        foreach (IProducer producer in producers)
        {
            _producers.Add(producer);
        }
    }

    private void SetConsumers(params IConsumer[] consumers)
    {
        foreach (IConsumer consumer in consumers)
        {
            _consumers.Add(consumer);
        }
    }

    private ConfluentAdminClientFactory GetRealFactory() =>
        new(_producers, _consumers, Substitute.For<ISilverbackLogger<ConfluentAdminClientFactory>>());

    private sealed record RecordedLog(LogLevel LogLevel, int EventId, string Message);

    // Records the logs written from the librdkafka threads
    private sealed class RecordingLogger : ILogger
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly ConcurrentQueue<RecordedLog> _logs = [];

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _logs.Enqueue(new RecordedLog(logLevel, eventId.Id, formatter(state, exception)));

        public async Task<RecordedLog> WaitForLogAsync(int eventId)
        {
            await AsyncTestingUtil.WaitAsync(() => _logs.Any(log => log.EventId == eventId), Timeout);

            return _logs.First(log => log.EventId == eventId);
        }
    }

    // Returns a fake client named after the client.id, as librdkafka names it (see GetClient_ShouldNameClientAfterClientId)
    private sealed class FakeAdminClientFactory : ConfluentAdminClientFactoryBase
    {
        public FakeAdminClientFactory(IProducerCollection producers, IConsumerCollection consumers)
            : base(producers, consumers)
        {
        }

        public override IAdminClient GetClient(AdminClientConfig config)
        {
            IAdminClient client = Substitute.For<IAdminClient>();
            client.Name.Returns(config.ClientId);

            return client;
        }
    }
}
