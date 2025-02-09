// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.HealthChecks.Messaging.HealthChecks;

public class OutboxHealthCheckFixture
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenQueueEmpty()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetMaxAgeAsync().Returns(Task.FromResult(TimeSpan.Zero));
        outboxReader.GetLengthAsync().Returns(Task.FromResult(0));

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddInMemoryOutbox())
                .Services
                .AddSingleton(outboxReader)
                .AddHealthChecks()
                .AddOutboxCheck());

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenQueueIsWithinLimits()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetMaxAgeAsync().Returns(Task.FromResult(TimeSpan.FromSeconds(5)));
        outboxReader.GetLengthAsync().Returns(Task.FromResult(42));

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddInMemoryOutbox())
                .Services
                .AddSingleton(outboxReader)
                .AddHealthChecks()
                .AddOutboxCheck(TimeSpan.FromSeconds(30), 100));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenQueueIsTooLong()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetMaxAgeAsync().Returns(Task.FromResult(TimeSpan.FromSeconds(5)));
        outboxReader.GetLengthAsync().Returns(Task.FromResult(101));

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddInMemoryOutbox())
                .Services
                .AddSingleton(outboxReader)
                .AddHealthChecks()
                .AddOutboxCheck(TimeSpan.FromSeconds(30), 100));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenQueueIsTooOld()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetMaxAgeAsync().Returns(Task.FromResult(TimeSpan.FromSeconds(31)));
        outboxReader.GetLengthAsync().Returns(Task.FromResult(42));

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddInMemoryOutbox())
                .Services
                .AddSingleton(outboxReader)
                .AddHealthChecks()
                .AddOutboxCheck(TimeSpan.FromSeconds(30), 100));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenExceptionThrown()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetMaxAgeAsync().Returns(Task.FromResult(TimeSpan.FromSeconds(31)));
        outboxReader.GetLengthAsync().ThrowsAsync(new ArithmeticException());

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddInMemoryOutbox())
                .Services
                .AddSingleton(outboxReader)
                .AddHealthChecks()
                .AddOutboxCheck(TimeSpan.FromSeconds(30), 100));

        (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeOfType<ArithmeticException>();
    }

    private static (IHealthCheck HealthCheck, HealthCheckContext Context) GetHealthCheck(IServiceProvider serviceProvider)
    {
        IServiceScope scope = serviceProvider.CreateScope();

        IOptions<HealthCheckServiceOptions> healthCheckOptions = scope.ServiceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        HealthCheckContext context = new()
        {
            Registration = healthCheckOptions.Value.Registrations.First()
        };

        return (context.Registration.Factory.Invoke(scope.ServiceProvider), context);
    }
}
