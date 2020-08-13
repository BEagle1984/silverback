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
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class BrokerConnectorServiceTests
    {
        [Fact]
        public async Task StartAsync_ConnectAtStartup_BrokersConnected()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => Substitute.For<IApplicationLifetime>())
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

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            var brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
            brokers.ForEach(broker => broker.IsConnected.Should().BeTrue());
        }

        [Fact]
        public async Task StartAsync_ExceptionWithRetryEnabled_ConnectRetried()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => Substitute.For<IApplicationLifetime>())
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

            var testBroker = serviceProvider.GetRequiredService<TestBroker>();
            testBroker.SimulateConnectIssues = true;

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            var brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
            brokers.ForEach(broker => broker.IsConnected.Should().BeTrue());

            testBroker.SimulateConnectIssues.Should().BeFalse(); // Check that the exception mechanism what triggered
        }

        [Fact]
        public async Task StartAsync_ExceptionWithRetryDisabled_ConnectNotRetried()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => Substitute.For<IApplicationLifetime>())
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

            var testBroker = serviceProvider.GetRequiredService<TestBroker>();
            testBroker.SimulateConnectIssues = true;

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            testBroker.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task StartAsync_ConnectAfterStartup_BrokerConnectedAfterLifetimeEvent()
        {
            var appStartedTokenSource = new CancellationTokenSource();
            var lifetimeEvents = Substitute.For<IApplicationLifetime>();
            lifetimeEvents.ApplicationStarted.Returns(appStartedTokenSource.Token);

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => lifetimeEvents)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .WithConnectionOptions(
                                new BrokerConnectionOptions
                                {
                                    Mode = BrokerConnectionMode.AfterStartup
                                })));

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            var testBroker = serviceProvider.GetRequiredService<TestBroker>();
            testBroker.IsConnected.Should().BeFalse();

            appStartedTokenSource.Cancel();

            testBroker.IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_AutoConnectDisabled_BrokersNotConnected()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => Substitute.For<IApplicationLifetime>())
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

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            var brokers = serviceProvider.GetRequiredService<IBrokerCollection>();
            brokers.ForEach(broker => broker.IsConnected.Should().BeFalse());
        }

        [Theory]
        [InlineData(BrokerConnectionMode.Manual)]
        [InlineData(BrokerConnectionMode.Startup)]
        [InlineData(BrokerConnectionMode.AfterStartup)]
        public async Task StartAsync_ApplicationStopping_BrokerGracefullyDisconnectedRegardlessOfMode(
            BrokerConnectionMode mode)
        {
            var appStoppingTokenSource = new CancellationTokenSource();
            var lifetimeEvents = Substitute.For<IApplicationLifetime>();
            lifetimeEvents.ApplicationStopping.Returns(appStoppingTokenSource.Token);

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddTransient(_ => lifetimeEvents)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .WithConnectionOptions(
                                new BrokerConnectionOptions
                                {
                                    Mode = mode
                                })));

            var service = serviceProvider.GetServices<IHostedService>().OfType<BrokerConnectorService>().Single();
            await service.StartAsync(CancellationToken.None);

            var testBroker = serviceProvider.GetRequiredService<TestBroker>();
            testBroker.Connect();
            testBroker.IsConnected.Should().BeTrue();

            appStoppingTokenSource.Cancel();

            testBroker.IsConnected.Should().BeFalse();
        }
    }
}
