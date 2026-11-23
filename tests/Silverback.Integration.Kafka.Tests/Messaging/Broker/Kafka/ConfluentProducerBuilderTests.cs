// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Tests.Integration.Kafka.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

// The producers are built against a broker that is always down, on the loopback interface
public class ConfluentProducerBuilderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Build_ShouldThrow_WhenConfigIsNotSet()
    {
        ConfluentProducerBuilder builder = new();

        Action act = () => builder.Build();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("SetConfig must be called to provide the producer configuration.");
    }

    [Fact]
    public async Task Build_ShouldBuildProducerWithConfig_WhenNoHandlerIsSet()
    {
        await using DisconnectingLoopbackServer server = new();
        ConfluentProducerBuilder builder = new();
        builder.SetConfiguration(GetConfig(server.BootstrapServers));

        using IProducer<byte[]?, byte[]?> producer = builder.Build();

        producer.Name.ShouldStartWith("client1#producer-");
        producer.Flush(TimeSpan.Zero).ShouldBe(0);
    }

    [Fact]
    public async Task Build_ShouldSetStatisticsHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IProducer<byte[]?, byte[]?> Producer, string Statistics)> statisticsReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProducerConfig config = GetConfig(server.BootstrapServers);
        config.StatisticsIntervalMs = 100;
        ConfluentProducerBuilder builder = new();
        builder
            .SetConfiguration(config)
            .SetStatisticsHandler((producer, statistics) => statisticsReceived.TrySetResult((producer, statistics)));

        using IProducer<byte[]?, byte[]?> producer = builder.Build();

        (IProducer<byte[]?, byte[]?> handlerProducer, string receivedStatistics) = await statisticsReceived.Task.WaitAsync(Timeout);
        handlerProducer.ShouldBeSameAs(producer);
        using JsonDocument statisticsDocument = JsonDocument.Parse(receivedStatistics);
        statisticsDocument.RootElement.GetProperty("type").GetString().ShouldBe("producer");
        statisticsDocument.RootElement.GetProperty("client_id").GetString().ShouldBe("client1");
    }

    [Fact]
    public async Task Build_ShouldSetLogHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IProducer<byte[]?, byte[]?> Producer, LogMessage LogMessage)> logReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProducerConfig config = GetConfig(server.BootstrapServers);
        config.Debug = "generic";
        ConfluentProducerBuilder builder = new();
        builder
            .SetConfiguration(config)
            .SetLogHandler((producer, logMessage) => logReceived.TrySetResult((producer, logMessage)));

        using IProducer<byte[]?, byte[]?> producer = builder.Build();

        (IProducer<byte[]?, byte[]?> handlerProducer, LogMessage receivedLogMessage) = await logReceived.Task.WaitAsync(Timeout);
        handlerProducer.ShouldBeSameAs(producer);
        receivedLogMessage.Level.ShouldBe(SyslogLevel.Debug);
        receivedLogMessage.Name.ShouldBe(producer.Name);
    }

    [Fact]
    public async Task Build_ShouldSetErrorHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IProducer<byte[]?, byte[]?> Producer, Error Error)> allBrokersDownReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfluentProducerBuilder builder = new();
        builder
            .SetConfiguration(GetConfig(server.BootstrapServers))
            .SetErrorHandler((producer, error) =>
            {
                if (error.Code == ErrorCode.Local_AllBrokersDown)
                    allBrokersDownReceived.TrySetResult((producer, error));
            });

        using IProducer<byte[]?, byte[]?> producer = builder.Build();

        (IProducer<byte[]?, byte[]?> handlerProducer, Error receivedError) = await allBrokersDownReceived.Task.WaitAsync(Timeout);
        handlerProducer.ShouldBeSameAs(producer);
        receivedError.IsFatal.ShouldBeFalse();
    }

    private static ProducerConfig GetConfig(string bootstrapServers) =>
        new()
        {
            BootstrapServers = bootstrapServers,
            ClientId = "client1"
        };
}
