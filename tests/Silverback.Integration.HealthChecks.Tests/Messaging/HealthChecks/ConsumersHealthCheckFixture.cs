// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.HealthChecks.Messaging.HealthChecks;

public class ConsumersHealthCheckFixture
{
    [Theory]
    [InlineData(ConsumerStatus.Connected)]
    [InlineData(ConsumerStatus.Consuming)]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenConsumerConnectedOrConsuming(ConsumerStatus status)
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(status);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Theory]
    [InlineData(ConsumerStatus.Stopped)]
    [InlineData(ConsumerStatus.Started)]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenConsumerStoppedOrStarted(ConsumerStatus status)
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(status);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldAppendDisplayNameToDescription()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Stopped);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.DisplayName.Returns("whatever-the-display-name");

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe(
            $"One or more consumers are not connected:{Environment.NewLine}" +
            "- whatever-the-display-name");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnCustomFailureStatus()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Stopped);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck(failureStatus: HealthStatus.Degraded));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Theory]
    [InlineData(ConsumerStatus.Stopped, HealthStatus.Unhealthy)]
    [InlineData(ConsumerStatus.Started, HealthStatus.Healthy)]
    [InlineData(ConsumerStatus.Connected, HealthStatus.Healthy)]
    [InlineData(ConsumerStatus.Consuming, HealthStatus.Healthy)]
    public async Task CheckHealthAsync_ShouldReturnStatusAccordingToMinConsumerStatus(
        ConsumerStatus consumerStatus,
        HealthStatus expectedHealthCheckStatus)
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(consumerStatus);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck(minHealthyStatus: ConsumerStatus.Started));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(expectedHealthCheckStatus);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenRevertedWithinGracePeriod()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Started);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
        [
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-300)),
            new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-120)),
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-25))
        ]);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenGracePeriodElapsed()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Started);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
        [
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-300)),
            new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-120)),
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-35))
        ]);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldRespectCustomGracePeriod()
    {
        IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(ConsumerStatus.Started);
        IConsumer? consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);
        consumer.StatusInfo.History.Returns(
        [
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-300)),
            new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-120)),
            new ConsumerStatusChange(ConsumerStatus.Started, DateTime.UtcNow.AddSeconds(-35))
        ]);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSilverback()
                .Services
                .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
                .AddHealthChecks()
                .AddConsumersCheck(gracePeriod: TimeSpan.FromSeconds(40)));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    private static (IHealthCheck HealthCheck, HealthCheckContext Context) GetHealthCheck(IServiceProvider serviceProvider)
    {
        IOptions<HealthCheckServiceOptions> healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        HealthCheckContext context = new()
        {
            Registration = healthCheckOptions.Value.Registrations.First()
        };

        return (context.Registration.Factory.Invoke(serviceProvider), context);
    }
}
