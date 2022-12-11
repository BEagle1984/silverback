// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.HealthChecks.Messaging.HealthChecks;

public class ConsumersHealthCheckTests
{
    // TODO: REIMPLEMENT
    // [Fact]
    // public async Task CheckHealthAsync_ConsumerReady_HealthyReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Ready);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck());
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Healthy);
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_ConsumerNotReady_UnhealthyReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
    //     consumer.Configuration.Returns(new TestConsumerConfiguration("topic1"));
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck());
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Unhealthy);
    //     result.Description.Should().Be(
    //         $"One or more consumers are not connected:{Environment.NewLine}" +
    //         "- topic1 [00000000-0000-0000-0000-000000000000]");
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_ConsumerWithFriendlyName_FriendlyNameAddedToDescription()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
    //     consumer.Configuration.Returns(
    //         new TestConsumerConfiguration("topic1")
    //         {
    //             FriendlyName = "friendly-one"
    //         });
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck());
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Unhealthy);
    //     result.Description.Should().Be(
    //         $"One or more consumers are not connected:{Environment.NewLine}" +
    //         "- friendly-one (topic1) [00000000-0000-0000-0000-000000000000]");
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_WithCustomFailureStatus_CorrectStatusReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
    //     consumer.Configuration.Returns(new TestConsumerConfiguration("topic1"));
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck(failureStatus: HealthStatus.Degraded));
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Degraded);
    //     result.Description.Should().Be(
    //         $"One or more consumers are not connected:{Environment.NewLine}" +
    //         "- topic1 [00000000-0000-0000-0000-000000000000]");
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_WithCustomMinConsumerStatus_CorrectStatusReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck(ConsumerStatus.Connected));
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Healthy);
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_GracePeriod_HealthyReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.StatusInfo.History.Returns(
    //         new List<IConsumerStatusChange>
    //         {
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
    //             new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-25))
    //         });
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck());
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Healthy);
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_ElapsedGracePeriod_UnhealthyReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.StatusInfo.History.Returns(
    //         new List<IConsumerStatusChange>
    //         {
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
    //             new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-35))
    //         });
    //     consumer.Id.Returns(new InstanceIdentifier(Guid.Empty));
    //     consumer.Configuration.Returns(new TestConsumerConfiguration("topic1"));
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck());
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Unhealthy);
    //     result.Description.Should().Be(
    //         $"One or more consumers are not connected:{Environment.NewLine}" +
    //         "- topic1 [00000000-0000-0000-0000-000000000000]");
    // }
    //
    // [Fact]
    // public async Task CheckHealthAsync_WithCustomGracePeriod_CorrectStatusReturned()
    // {
    //     IConsumerStatusInfo? statusInfo = Substitute.For<IConsumerStatusInfo>();
    //     statusInfo.Status.Returns(ConsumerStatus.Connected);
    //     IConsumer? consumer = Substitute.For<IConsumer>();
    //     consumer.StatusInfo.Returns(statusInfo);
    //     consumer.StatusInfo.History.Returns(
    //         new List<IConsumerStatusChange>
    //         {
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-300)),
    //             new ConsumerStatusChange(ConsumerStatus.Ready, DateTime.UtcNow.AddSeconds(-120)),
    //             new ConsumerStatusChange(ConsumerStatus.Connected, DateTime.UtcNow.AddSeconds(-25))
    //         });
    //     consumer.Configuration.Returns(new TestConsumerConfiguration("topic1"));
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     broker.Consumers.Returns(new[] { consumer });
    //
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddSilverback()
    //             .Services
    //             .AddSingleton<IBrokerCollection>(new BrokerCollection(new[] { broker }))
    //             .AddHealthChecks()
    //             .AddConsumersCheck(gracePeriod: TimeSpan.FromSeconds(5)));
    //
    //     (IHealthCheck healthCheck, HealthCheckContext context) = GetHealthCheck(serviceProvider);
    //
    //     HealthCheckResult result = await healthCheck.CheckHealthAsync(context);
    //
    //     result.Status.Should().Be(HealthStatus.Unhealthy);
    // }
    //
    // private static (IHealthCheck HealthCheck, HealthCheckContext Context) GetHealthCheck(IServiceProvider serviceProvider)
    // {
    //     IOptions<HealthCheckServiceOptions> healthCheckOptions = serviceProvider
    //         .GetRequiredService<IOptions<HealthCheckServiceOptions>>();
    //
    //     HealthCheckContext context = new()
    //     {
    //         Registration = healthCheckOptions.Value.Registrations.First()
    //     };
    //
    //     return (context.Registration.Factory.Invoke(serviceProvider), context);
    // }
}
