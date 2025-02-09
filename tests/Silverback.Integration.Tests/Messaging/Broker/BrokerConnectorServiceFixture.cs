// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerConnectorServiceFixture
{
    [Fact]
    public async Task StartAsync_ShouldConnectAllClients_WhenModeIsConnectAtStartup()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.ConnectAtStartup()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        foreach (IBrokerClient client in clients)
        {
            await client.Received(1).ConnectAsync();
        }
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task StartAsync_ShouldRetry_WhenExceptionIsThrownAndRetryIsEnabled()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.ConnectAtStartup().RetryOnConnectionFailure(TimeSpan.FromMilliseconds(100))));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();

        int tries = 0;
        IBrokerClient brokenClient = Substitute.For<IBrokerClient>();
        brokenClient.ConnectAsync().ReturnsForAnyArgs(ValueTask.CompletedTask).AndDoes(
            _ =>
            {
                if (++tries < 3)
                    throw new InvalidOperationException("retry!");
            });

        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(brokenClient);
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        tries.ShouldBe(3);

        foreach (IBrokerClient client in clients)
        {
            await client.Received(3).ConnectAsync();
        }
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task StartAsync_ShouldNotRetry_WhenExceptionIsThrownAndRetryIsDisabled()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.ConnectAtStartup().DisableRetryOnConnectionFailure()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();

        int tries = 0;
        IBrokerClient brokenClient = Substitute.For<IBrokerClient>();
        brokenClient.ConnectAsync().ReturnsForAnyArgs(ValueTask.CompletedTask).AndDoes(
            _ =>
            {
                if (++tries < 3)
                    throw new InvalidOperationException("retry!");
            });

        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(brokenClient);
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        tries.ShouldBe(1);

        foreach (IBrokerClient client in clients)
        {
            await client.Received(1).ConnectAsync();
        }
    }

    [Fact]
    public async Task StartAsync_ShouldConnectAllClientsAfterApplicationStartup_WhenModeIsAfterStartup()
    {
        CancellationTokenSource appStartedTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStarted.Returns(appStartedTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => lifetimeEvents)
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.ConnectAfterStartup()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        foreach (IBrokerClient client in clients)
        {
            await client.Received(0).ConnectAsync();
        }

        appStartedTokenSource.Cancel();

        foreach (IBrokerClient client in clients)
        {
            await client.Received(1).ConnectAsync();
        }
    }

    [Fact]
    public async Task StartAsync_ShouldNotConnectClients_WhenModeIsManual()
    {
        CancellationTokenSource appStartedTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStarted.Returns(appStartedTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => lifetimeEvents)
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.ManuallyConnect()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        appStartedTokenSource.Cancel();

        foreach (IBrokerClient client in clients)
        {
            await client.Received(0).ConnectAsync();
        }
    }

    [Theory]
    [InlineData(BrokerClientConnectionMode.Manual)]
    [InlineData(BrokerClientConnectionMode.Startup)]
    [InlineData(BrokerClientConnectionMode.AfterStartup)]
    public async Task StartAsync_ShouldAlwaysSetupGracefulDisconnectRegardlessOfMode(BrokerClientConnectionMode mode)
    {
        CancellationTokenSource appStoppingTokenSource = new();
        CancellationTokenSource appStoppedTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStopping.Returns(appStoppingTokenSource.Token);
        lifetimeEvents.ApplicationStopped.Returns(appStoppedTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => lifetimeEvents)
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options.WithConnectionOptions(
                        new BrokerClientConnectionOptions
                        {
                            Mode = mode
                        })));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());
        clients.Add(Substitute.For<IBrokerClient>());

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        appStoppingTokenSource.Cancel();
        appStoppedTokenSource.Cancel();

        foreach (IBrokerClient client in clients)
        {
            await client.Received(1).DisconnectAsync();
        }
    }
}
