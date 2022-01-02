// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerConnectorServiceTests
{
    [Fact]
    public async Task StartAsync_ConnectAtStartup_BrokersConnected()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = BrokerConnectionMode.Startup
                            })));

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        IBrokerCollection brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
        brokers.ForEach(broker => broker.IsConnected.Should().BeTrue());
    }

    [Fact]
    public async Task StartAsync_ExceptionWithRetryEnabled_ConnectRetried()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = BrokerConnectionMode.Startup,
                                RetryOnFailure = true,
                                RetryInterval = TimeSpan.FromMilliseconds(1)
                            })));

        TestBroker testBroker = serviceProvider.GetRequiredService<TestBroker>();
        testBroker.SimulateConnectIssues = true;

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        IBrokerCollection brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
        brokers.ForEach(broker => broker.IsConnected.Should().BeTrue());

        testBroker.SimulateConnectIssues.Should().BeFalse(); // Check that the exception mechanism what triggered
    }

    [Fact]
    public async Task StartAsync_ExceptionWithRetryDisabled_ConnectNotRetried()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = BrokerConnectionMode.Startup,
                                RetryOnFailure = false
                            })));

        TestBroker testBroker = serviceProvider.GetRequiredService<TestBroker>();
        testBroker.SimulateConnectIssues = true;

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        testBroker.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ConnectAfterStartup_BrokerConnectedAfterLifetimeEvent()
    {
        CancellationTokenSource appStartedTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStarted.Returns(appStartedTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => lifetimeEvents)
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = BrokerConnectionMode.AfterStartup
                            })));

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        TestBroker testBroker = serviceProvider.GetRequiredService<TestBroker>();
        testBroker.IsConnected.Should().BeFalse();

        appStartedTokenSource.Cancel();

        testBroker.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_AutoConnectDisabled_BrokersNotConnected()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = BrokerConnectionMode.Manual
                            })));

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        IBrokerCollection brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
        brokers.ForEach(broker => broker.IsConnected.Should().BeFalse());
    }

    [Theory]
    [InlineData(BrokerConnectionMode.Manual)]
    [InlineData(BrokerConnectionMode.Startup)]
    [InlineData(BrokerConnectionMode.AfterStartup)]
    public async Task StartAsync_ApplicationStopping_BrokerGracefullyDisconnectedRegardlessOfMode(BrokerConnectionMode mode)
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
                    options => options
                        .AddBroker<TestBroker>()
                        .WithConnectionOptions(
                            new BrokerConnectionOptions
                            {
                                Mode = mode
                            })));

        BrokerConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        TestBroker testBroker = serviceProvider.GetRequiredService<TestBroker>();
        await testBroker.ConnectAsync();
        testBroker.IsConnected.Should().BeTrue();

        appStoppingTokenSource.Cancel();
        appStoppedTokenSource.Cancel();

        testBroker.IsConnected.Should().BeFalse();
    }
}
