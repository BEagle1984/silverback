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

namespace Silverback.Tests.Integration.Messaging.HealthChecks
{
    public class ConsumersHealthCheckServiceTests
    {
        private readonly IConsumer _disconnectedConsumer;

        private readonly IConsumer _connectedConsumer;

        private readonly IConsumer _readyConsumer;

        private readonly IConsumer _consumingConsumer;

        public ConsumersHealthCheckServiceTests()
        {
            var disconnectedStatusInfo = Substitute.For<IConsumerStatusInfo>();
            disconnectedStatusInfo.Status.Returns(ConsumerStatus.Disconnected);
            _disconnectedConsumer = Substitute.For<IConsumer>();
            _disconnectedConsumer.StatusInfo.Returns(disconnectedStatusInfo);

            var connectedStatusInfo = Substitute.For<IConsumerStatusInfo>();
            connectedStatusInfo.Status.Returns(ConsumerStatus.Connected);
            _connectedConsumer = Substitute.For<IConsumer>();
            _connectedConsumer.StatusInfo.Returns(connectedStatusInfo);

            var readyStatusInfo = Substitute.For<IConsumerStatusInfo>();
            readyStatusInfo.Status.Returns(ConsumerStatus.Ready);
            _readyConsumer = Substitute.For<IConsumer>();
            _readyConsumer.StatusInfo.Returns(readyStatusInfo);

            var consumingStatusInfo = Substitute.For<IConsumerStatusInfo>();
            consumingStatusInfo.Status.Returns(ConsumerStatus.Consuming);
            _consumingConsumer = Substitute.For<IConsumer>();
            _consumingConsumer.StatusInfo.Returns(consumingStatusInfo);
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_AllConsumersConnected_EmptyCollectionReturned()
        {
            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(
                new[]
                {
                    _connectedConsumer, _consumingConsumer, _readyConsumer
                });
            var broker2 = Substitute.For<IBroker>();
            broker2.ProducerEndpointType.Returns(typeof(TestOtherProducerEndpoint));
            broker2.ConsumerEndpointType.Returns(typeof(TestOtherConsumerEndpoint));
            broker2.Consumers.Returns(
                new[]
                {
                    _readyConsumer, _readyConsumer
                });

            var brokerCollection = new BrokerCollection(new[] { broker1, broker2 });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Connected);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_SomeConsumersNotFullyConnected_ConsumersListReturned()
        {
            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(
                new[]
                {
                    _readyConsumer, _consumingConsumer, _connectedConsumer
                });
            var broker2 = Substitute.For<IBroker>();
            broker2.ProducerEndpointType.Returns(typeof(TestOtherProducerEndpoint));
            broker2.ConsumerEndpointType.Returns(typeof(TestOtherConsumerEndpoint));
            broker2.Consumers.Returns(
                new[]
                {
                    _readyConsumer, _disconnectedConsumer
                });

            var brokerCollection = new BrokerCollection(new[] { broker1, broker2 });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready);

            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(_connectedConsumer, _disconnectedConsumer);
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_ShuttingDown_EmptyCollectionReturned()
        {
            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(
                new[]
                {
                    _readyConsumer, _consumingConsumer, _connectedConsumer
                });
            var broker2 = Substitute.For<IBroker>();
            broker2.ProducerEndpointType.Returns(typeof(TestOtherProducerEndpoint));
            broker2.ConsumerEndpointType.Returns(typeof(TestOtherConsumerEndpoint));
            broker2.Consumers.Returns(
                new[]
                {
                    _readyConsumer, _disconnectedConsumer
                });

            var brokerCollection = new BrokerCollection(new[] { broker1, broker2 });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var applicationStoppingTokenSource = new CancellationTokenSource();
            hostApplicationLifetime.ApplicationStopping.Returns(applicationStoppingTokenSource.Token);
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            applicationStoppingTokenSource.Cancel();

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_GracePeriod_EmptyCollectionReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-30)),
                    new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-20)),
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-5)),
                });

            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var brokerCollection = new BrokerCollection(new[] { broker });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready, TimeSpan.FromSeconds(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_NeverConnected_ConsumersListReturnedDespiteGracePeriod()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-5)),
                });

            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var brokerCollection = new BrokerCollection(new[] { broker });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready, TimeSpan.FromSeconds(10));

            result.Should().HaveCount(1);
            result.Should().BeEquivalentTo(consumer);
        }

        [Fact]
        public async Task GetDisconnectedConsumersAsync_ElapsedGracePeriod_ConsumersListReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-30)),
                    new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-20)),
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-15)),
                });

            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var brokerCollection = new BrokerCollection(new[] { broker });
            var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            var service = new ConsumersHealthCheckService(brokerCollection, hostApplicationLifetime);

            IReadOnlyCollection<IConsumer> result =
                await service.GetDisconnectedConsumersAsync(ConsumerStatus.Ready, TimeSpan.FromSeconds(10));

            result.Should().HaveCount(1);
            result.Should().BeEquivalentTo(consumer);
        }
    }
}
