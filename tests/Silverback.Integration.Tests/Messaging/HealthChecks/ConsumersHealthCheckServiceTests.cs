// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.HealthChecks;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks;

public class ConsumersHealthCheckServiceTests
{
    private readonly IConsumer _disconnectedConsumer;

    private readonly IConsumer _connectedConsumer;

    private readonly IConsumer _readyConsumer;

    private readonly IConsumer _consumingConsumer;

    public ConsumersHealthCheckServiceTests()
    {
        IConsumerStatusInfo? disconnectedStatusInfo = Substitute.For<IConsumerStatusInfo>();
        disconnectedStatusInfo.Status.Returns(ConsumerStatus.Disconnected);
        _disconnectedConsumer = Substitute.For<IConsumer>();
        _disconnectedConsumer.StatusInfo.Returns(disconnectedStatusInfo);
        _disconnectedConsumer.Configuration.Returns(new TestConsumerConfiguration("topic1"));

        IConsumerStatusInfo? connectedStatusInfo = Substitute.For<IConsumerStatusInfo>();
        connectedStatusInfo.Status.Returns(ConsumerStatus.Connected);
        _connectedConsumer = Substitute.For<IConsumer>();
        _connectedConsumer.StatusInfo.Returns(connectedStatusInfo);
        _connectedConsumer.Configuration.Returns(new TestConsumerConfiguration("topic2"));

        IConsumerStatusInfo? readyStatusInfo = Substitute.For<IConsumerStatusInfo>();
        readyStatusInfo.Status.Returns(ConsumerStatus.Ready);
        _readyConsumer = Substitute.For<IConsumer>();
        _readyConsumer.StatusInfo.Returns(readyStatusInfo);
        _readyConsumer.Configuration.Returns(new TestConsumerConfiguration("topic3"));

        IConsumerStatusInfo? consumingStatusInfo = Substitute.For<IConsumerStatusInfo>();
        consumingStatusInfo.Status.Returns(ConsumerStatus.Consuming);
        _consumingConsumer = Substitute.For<IConsumer>();
        _consumingConsumer.StatusInfo.Returns(consumingStatusInfo);
        _consumingConsumer.Configuration.Returns(new TestConsumerConfiguration("topic4"));
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_AllConsumersConnected_EmptyCollectionReturned()
    {
        IBroker? broker1 = Substitute.For<IBroker>();
        broker1.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker1.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker1.Consumers.Returns(
            new[]
            {
                _connectedConsumer, _consumingConsumer, _readyConsumer
            });
        IBroker? broker2 = Substitute.For<IBroker>();
        broker2.ProducerConfigurationType.Returns(typeof(TestOtherProducerConfiguration));
        broker2.ConsumerConfigurationType.Returns(typeof(TestOtherConsumerConfiguration));
        broker2.Consumers.Returns(
            new[]
            {
                _readyConsumer, _readyConsumer
            });

        BrokerCollection brokerCollection = new(new[] { broker1, broker2 });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result =
            await service.GetDisconnectedConsumersAsync(ConsumerStatus.Connected, TimeSpan.Zero, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_SomeConsumersNotFullyConnected_ConsumersListReturned()
    {
        IBroker? broker1 = Substitute.For<IBroker>();
        broker1.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker1.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker1.Consumers.Returns(
            new[]
            {
                _readyConsumer, _consumingConsumer, _connectedConsumer
            });
        IBroker? broker2 = Substitute.For<IBroker>();
        broker2.ProducerConfigurationType.Returns(typeof(TestOtherProducerConfiguration));
        broker2.ConsumerConfigurationType.Returns(typeof(TestOtherConsumerConfiguration));
        broker2.Consumers.Returns(
            new[]
            {
                _readyConsumer, _disconnectedConsumer
            });

        BrokerCollection brokerCollection = new(new[] { broker1, broker2 });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result =
            await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready, TimeSpan.Zero, null);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(new[] { _connectedConsumer, _disconnectedConsumer });
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShuttingDown_EmptyCollectionReturned()
    {
        IBroker? broker1 = Substitute.For<IBroker>();
        broker1.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker1.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker1.Consumers.Returns(
            new[]
            {
                _readyConsumer, _consumingConsumer, _connectedConsumer
            });
        IBroker? broker2 = Substitute.For<IBroker>();
        broker2.ProducerConfigurationType.Returns(typeof(TestOtherProducerConfiguration));
        broker2.ConsumerConfigurationType.Returns(typeof(TestOtherConsumerConfiguration));
        broker2.Consumers.Returns(
            new[]
            {
                _readyConsumer, _disconnectedConsumer
            });

        BrokerCollection brokerCollection = new(new[] { broker1, broker2 });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        CancellationTokenSource applicationStoppingTokenSource = new();
        hostApplicationLifetime.ApplicationStopping.Returns(applicationStoppingTokenSource.Token);
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        applicationStoppingTokenSource.Cancel();

        IReadOnlyCollection<IConsumer> result =
            await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready, TimeSpan.Zero, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_GracePeriod_EmptyCollectionReturned()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Connected);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
            new List<IConsumerStatusChange>
            {
                new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-30)),
                new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-20)),
                new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-5))
            });

        IBroker? broker = Substitute.For<IBroker>();
        broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker.Consumers.Returns(new[] { consumer });

        BrokerCollection brokerCollection = new(new[] { broker });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result =
            await service.GetDisconnectedConsumersAsync(
                ConsumerStatus.Ready,
                TimeSpan.FromSeconds(10),
                null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task
        GetDisconnectedConsumersAsync_NeverFullyConnected_ConsumersListReturnedAfterGracePeriod()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Connected);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
            new List<IConsumerStatusChange>
            {
                new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow)
            });

        IBroker? broker = Substitute.For<IBroker>();
        broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker.Consumers.Returns(new[] { consumer });

        BrokerCollection brokerCollection = new(new[] { broker });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Ready,
            TimeSpan.FromMilliseconds(100),
            null);

        result.Should().BeEmpty();

        await Task.Delay(100);

        result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Ready,
            TimeSpan.FromMilliseconds(100),
            null);

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(new[] { consumer });
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ElapsedGracePeriod_ConsumersListReturned()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Connected);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
            new List<IConsumerStatusChange>
            {
                new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-30)),
                new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-20)),
                new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-15))
            });

        IBroker? broker = Substitute.For<IBroker>();
        broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker.Consumers.Returns(new[] { consumer });

        BrokerCollection brokerCollection = new(new[] { broker });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result =
            await service.GetDisconnectedConsumersAsync(
                ConsumerStatus.Ready,
                TimeSpan.FromSeconds(10),
                null);

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(new[] { consumer });
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_Filter_FilteredConsumersListReturned()
    {
        IBroker? broker = Substitute.For<IBroker>();
        broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
        broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
        broker.Consumers.Returns(
            new[]
            {
                _disconnectedConsumer, _connectedConsumer
            });

        BrokerCollection brokerCollection = new(new[] { broker });
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(brokerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result1 =
            await service.GetDisconnectedConsumersAsync(
                ConsumerStatus.Ready,
                TimeSpan.Zero,
                endpoint => endpoint.RawName is "topic1" or "topic2");

        IReadOnlyCollection<IConsumer> result2 =
            await service.GetDisconnectedConsumersAsync(
                ConsumerStatus.Ready,
                TimeSpan.Zero,
                endpoint => endpoint.RawName == "topic1");

        result1.Should().HaveCount(2);
        result1.Should().BeEquivalentTo(new[] { _disconnectedConsumer, _connectedConsumer });
        result2.Should().HaveCount(1);
        result2.Should().BeEquivalentTo(new[] { _disconnectedConsumer });
    }
}
