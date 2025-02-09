// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.HealthChecks;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks;

public class ConsumersHealthCheckServiceFixture
{
    private readonly IConsumer _stoppedConsumer;

    private readonly IConsumer _startedConsumer;

    private readonly IConsumer _connectedConsumer;

    private readonly IConsumer _consumingConsumer;

    public ConsumersHealthCheckServiceFixture()
    {
        IConsumerStatusInfo? stoppedStatusInfo = Substitute.For<IConsumerStatusInfo>();
        stoppedStatusInfo.Status.Returns(ConsumerStatus.Stopped);
        stoppedStatusInfo.History.Returns([new ConsumerStatusChange(ConsumerStatus.Stopped, DateTime.UtcNow)]);
        _stoppedConsumer = Substitute.For<IConsumer>();
        _stoppedConsumer.StatusInfo.Returns(stoppedStatusInfo);
        _stoppedConsumer.EndpointsConfiguration.Returns([new TestConsumerEndpointConfiguration("topic1")]);

        IConsumerStatusInfo? startedStatusInfo = Substitute.For<IConsumerStatusInfo>();
        startedStatusInfo.Status.Returns(ConsumerStatus.Started);
        startedStatusInfo.History.Returns([new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow)]);
        _startedConsumer = Substitute.For<IConsumer>();
        _startedConsumer.StatusInfo.Returns(startedStatusInfo);
        _startedConsumer.EndpointsConfiguration.Returns([new TestConsumerEndpointConfiguration("topic2")]);

        IConsumerStatusInfo? connectedStatusInfo = Substitute.For<IConsumerStatusInfo>();
        connectedStatusInfo.Status.Returns(ConsumerStatus.Connected);
        connectedStatusInfo.History.Returns([new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow)]);
        _connectedConsumer = Substitute.For<IConsumer>();
        _connectedConsumer.StatusInfo.Returns(connectedStatusInfo);
        _connectedConsumer.EndpointsConfiguration.Returns([new TestConsumerEndpointConfiguration("topic3")]);

        IConsumerStatusInfo? consumingStatusInfo = Substitute.For<IConsumerStatusInfo>();
        consumingStatusInfo.Status.Returns(ConsumerStatus.Consuming);
        consumingStatusInfo.History.Returns([new ConsumerStatusChange(ConsumerStatus.Consuming, DateTime.UtcNow)]);
        _consumingConsumer = Substitute.For<IConsumer>();
        _consumingConsumer.StatusInfo.Returns(consumingStatusInfo);
        _consumingConsumer.EndpointsConfiguration.Returns([new TestConsumerEndpointConfiguration("topic4")]);
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShouldReturnEmptyCollection_WhenAllConsumersConnected()
    {
        ConsumerCollection consumerCollection = [_connectedConsumer, _consumingConsumer];
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(consumerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.Zero);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShouldReturnCollection_WhenSomeConsumersDisconnected()
    {
        ConsumerCollection consumerCollection = [_stoppedConsumer, _startedConsumer, _connectedConsumer, _consumingConsumer];
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(consumerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.Zero);

        result.Count.ShouldBe(2);
        result.ShouldBe([_stoppedConsumer, _startedConsumer]);
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShouldReturnEmptyCollection_WhenApplicationStopping()
    {
        ConsumerCollection consumerCollection = [_stoppedConsumer, _startedConsumer, _connectedConsumer, _consumingConsumer];
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        CancellationTokenSource applicationStoppingTokenSource = new();
        hostApplicationLifetime.ApplicationStopping.Returns(applicationStoppingTokenSource.Token);
        ConsumersHealthCheckService service = new(consumerCollection, hostApplicationLifetime);

        applicationStoppingTokenSource.Cancel();

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.Zero);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShouldReturnEmptyCollection_WhenGracePeriodNotElapsed()
    {
        ConsumerCollection consumerCollection = [_stoppedConsumer, _startedConsumer, _connectedConsumer, _consumingConsumer];
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(consumerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.FromSeconds(10));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDisconnectedConsumersAsync_ShouldReturnCollection_WhenGracePeriodElapsed()
    {
        ConsumerCollection consumerCollection = [_stoppedConsumer, _startedConsumer, _connectedConsumer, _consumingConsumer];
        IHostApplicationLifetime? hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ConsumersHealthCheckService service = new(consumerCollection, hostApplicationLifetime);

        IReadOnlyCollection<IConsumer> result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.FromMilliseconds(1000));

        result.ShouldBeEmpty();

        await Task.Delay(150);

        result = await service.GetDisconnectedConsumersAsync(
            ConsumerStatus.Connected,
            TimeSpan.FromMilliseconds(100));

        result.Count.ShouldBe(2);
        result.ShouldBe([_stoppedConsumer, _startedConsumer]);
    }
}
