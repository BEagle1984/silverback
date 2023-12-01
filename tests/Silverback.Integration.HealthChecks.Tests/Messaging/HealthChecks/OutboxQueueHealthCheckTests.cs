// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Silverback.Database;
using Silverback.Messaging.HealthChecks;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Xunit;

namespace Silverback.Tests.Integration.HealthChecks.Messaging.HealthChecks
{
    public class OutboxQueueHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_ExceptionThrown_UnhealthyReturned()
        {
            var outboundQueueHealthCheckService = Substitute.For<IOutboundQueueHealthCheckService>();
            outboundQueueHealthCheckService.CheckIsHealthyAsync(Arg.Any<TimeSpan>()).ThrowsAsync(new TimeoutException());

            var outboundQueueHealthCheck = new OutboxQueueHealthCheck(outboundQueueHealthCheckService);

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options
                        .AddKafka()
                        .AddOutbox<DbOutboxWriter, DbOutboxReader>())
                    .Services
                    .AddSingleton(outboundQueueHealthCheckService)
                    .AddSingleton(Substitute.For<IDbContext>())
                    .AddHealthChecks()
                    .Add(new HealthCheckRegistration("OutboundQueue", outboundQueueHealthCheck, default, default)));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [Fact]
        public async Task CheckHealthAsync_ExceptionThrown_ExceptionReturned()
        {
            var outboundQueueHealthCheckService = Substitute.For<IOutboundQueueHealthCheckService>();
            TimeoutException exceptionToBeThrown = new TimeoutException();
            outboundQueueHealthCheckService.CheckIsHealthyAsync(Arg.Any<TimeSpan>()).ThrowsAsync(exceptionToBeThrown);

            var outboundQueueHealthCheck = new OutboxQueueHealthCheck(outboundQueueHealthCheckService);

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options
                        .AddKafka()
                        .AddOutbox<DbOutboxWriter, DbOutboxReader>())
                    .Services
                    .AddSingleton(outboundQueueHealthCheckService)
                    .AddSingleton(Substitute.For<IDbContext>())
                    .AddHealthChecks()
                    .Add(new HealthCheckRegistration("OutboundQueue", outboundQueueHealthCheck, default, default)));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Exception.Should().Be(exceptionToBeThrown);
        }

        [Theory]
        [InlineData(true, HealthStatus.Healthy)]
        [InlineData(false, HealthStatus.Unhealthy)]
        public async Task CheckHealthAsync_HealthyCheck_ReturnsExpectedStatus(bool healthy, HealthStatus expectedStatus)
        {
            var outboundQueueHealthCheckService = Substitute.For<IOutboundQueueHealthCheckService>();
            outboundQueueHealthCheckService.CheckIsHealthyAsync(Arg.Any<TimeSpan>()).Returns(healthy);

            var outboundQueueHealthCheck = new OutboxQueueHealthCheck(outboundQueueHealthCheckService);

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options
                        .AddKafka()
                        .AddOutbox<DbOutboxWriter, DbOutboxReader>())
                    .Services
                    .AddSingleton(outboundQueueHealthCheckService)
                    .AddSingleton(Substitute.For<IDbContext>())
                    .AddHealthChecks()
                    .Add(new HealthCheckRegistration("OutboundQueue", outboundQueueHealthCheck, default, default)));

            var (healthCheck, context) = GetHealthCheck(serviceProvider);

            var result = await healthCheck.CheckHealthAsync(context);

            result.Status.Should().Be(expectedStatus);
        }

        private static (IHealthCheck HealthCheck, HealthCheckContext Context) GetHealthCheck(IServiceProvider serviceProvider)
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
