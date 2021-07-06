// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.HealthChecks.Messaging.HealthChecks
{
    public class ConsumersHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_ConsumerReady_HealthyReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Ready);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck());

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Fact]
        public async Task CheckHealthAsync_ConsumerNotReady_UnhealthyReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
            consumer.Endpoint.Returns(new TestConsumerEndpoint("topic1"));
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck());

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().StartWith(
                $"One or more consumers are not connected:{Environment.NewLine}" +
                "- [00000000-0000-0000-0000-000000000000] topic1");
        }

        [Fact]
        public async Task CheckHealthAsync_WithCustomFailureStatus_CorrectStatusReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
            consumer.Endpoint.Returns(new TestConsumerEndpoint("topic1"));
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck(failureStatus: HealthStatus.Degraded));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Degraded);
            result.Description.Should().StartWith(
                $"One or more consumers are not connected:{Environment.NewLine}" +
                "- [00000000-0000-0000-0000-000000000000] topic1");
        }

        [Fact]
        public async Task CheckHealthAsync_WithCustomMinConsumerStatus_CorrectStatusReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck(ConsumerStatus.Connected));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Fact]
        public async Task CheckHealthAsync_GracePeriod_HealthyReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
                    new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-25))
                });
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck());

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Fact]
        public async Task CheckHealthAsync_ElapsedGracePeriod_UnhealthyReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
                    new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-35))
                });
            consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
            consumer.Endpoint.Returns(new TestConsumerEndpoint("topic1"));
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck());

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().StartWith(
                $"One or more consumers are not connected:{Environment.NewLine}" +
                "- [00000000-0000-0000-0000-000000000000] topic1");
        }

        [Fact]
        public async Task CheckHealthAsync_WithCustomGracePeriod_CorrectStatusReturned()
        {
            var statusInfo = Substitute.For<IConsumerStatusInfo>();
            statusInfo.Status.Returns(ConsumerStatus.Connected);
            var consumer = Substitute.For<IConsumer>();
            consumer.StatusInfo.Returns(statusInfo);
            consumer.StatusInfo.History.Returns(
                new List<IConsumerStatusChange>
                {
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
                    new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
                    new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-25))
                });
            var broker = Substitute.For<IBroker>();
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker.Consumers.Returns(new[] { consumer });

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .Services
                    .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
                    .AddHealthChecks()
                    .AddConsumersCheck(TimeSpan.FromSeconds(5)));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        private static (IHealthCheck HealthCheck, HealthCheckContext Context) GetHealthCheck(
            IServiceProvider serviceProvider)
        {
            var healthCheckOptions = serviceProvider
                .GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            var context = new HealthCheckContext
            {
                Registration = healthCheckOptions.Value.Registrations.First()
            };

            return (context.Registration.Factory.Invoke(serviceProvider), context);
        }
    }
}
