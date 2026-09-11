// Copyright (c) 2026 Sergio Aquilini
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
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerClientsConnectorServiceTests
{
    [Fact]
    public void BrokerClientsConnector_ShouldBeSingleton()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        IBrokerClientsConnector connector1 = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        IBrokerClientsConnector connector2 = serviceProvider.GetRequiredService<IBrokerClientsConnector>();

        connector1.ShouldBeSameAs(connector2);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "The ValueTasks are converted to Tasks")]
    public async Task ConnectAsync_ShouldConnectClientsOnlyOnce_WhenCalledConcurrently()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        TaskCompletionSource connectCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Status.Returns(_ => connectCompletionSource.Task.IsCompletedSuccessfully ? ClientStatus.Initialized : ClientStatus.Disconnected);
        client.ConnectAsync().Returns(new ValueTask(connectCompletionSource.Task));
        clients.Add(client);

        IBrokerClientsConnector connector = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        Task connectTask1 = connector.ConnectAsync().AsTask();
        Task connectTask2 = connector.ConnectAsync().AsTask();
        connectCompletionSource.SetResult();
        await Task.WhenAll(connectTask1, connectTask2);
        await connector.ConnectAsync();

        await client.Received(1).ConnectAsync();
    }

    [Fact]
    public async Task ConnectAsync_ShouldReconnectClient_WhenItIsNoLongerInitialized()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        ClientStatus clientStatus = ClientStatus.Initialized;
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        client.Status.Returns(_ => clientStatus);
        clients.Add(client);

        IBrokerClientsConnector connector = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        await connector.ConnectAsync();
        clientStatus = ClientStatus.Disconnected;
        await connector.ConnectAsync();

        await client.Received(2).ConnectAsync();
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotReconnectClient_WhenItIsAlreadyInitializing()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        ClientStatus clientStatus = ClientStatus.Initialized;
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        client.Status.Returns(_ => clientStatus);
        clients.Add(client);

        IBrokerClientsConnector connector = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        await connector.ConnectAsync();
        clientStatus = ClientStatus.Initializing;
        await connector.ConnectAsync();

        await client.Received(1).ConnectAsync();
    }

    [Fact]
    public async Task ConnectAsync_ShouldConnectClientsAgain_AfterDisconnect()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        clients.Add(client);

        IBrokerClientsConnector connector = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        await connector.ConnectAsync();
        await connector.DisconnectAsync();
        await connector.ConnectAsync();

        await client.Received(2).ConnectAsync();
        await client.Received(1).DisconnectAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldConnectAllClients_WhenModeIsConnectAtStartup()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.ConnectAtStartup()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.ConnectAtStartup().RetryOnConnectionFailure(TimeSpan.FromMilliseconds(100))));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();

        int tries = 0;
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        client2.ConnectAsync().ReturnsForAnyArgs(ValueTask.CompletedTask).AndDoes(_ =>
        {
            if (++tries < 3)
                throw new InvalidOperationException("retry!");
        });
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.ConnectAtStartup().DisableRetryOnConnectionFailure()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();

        int tries = 0;
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        client2.ConnectAsync().ReturnsForAnyArgs(ValueTask.CompletedTask).AndDoes(_ =>
        {
            if (++tries < 3)
                throw new InvalidOperationException("retry!");
        });
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

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

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => lifetimeEvents)
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.ConnectAfterStartup()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

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

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => lifetimeEvents)
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.ManuallyConnect()));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

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
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStopping.Returns(appStoppingTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => lifetimeEvents)
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.WithConnectionOptions(
                new BrokerClientConnectionOptions
                {
                    Mode = mode
                })));

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();

        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        clients.Add(client1);
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");
        clients.Add(client2);
        IBrokerClient client3 = Substitute.For<IBrokerClient>();
        client3.Name.Returns("client3");
        clients.Add(client3);

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        appStoppingTokenSource.Cancel();
        await service.StoppedAsync(CancellationToken.None);

        foreach (IBrokerClient client in clients)
        {
            await client.Received(1).DisconnectAsync();
        }
    }

    [Fact]
    public async Task StoppedAsync_ShouldDisconnectAllClients()
    {
        CancellationTokenSource appStoppingTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStopping.Returns(appStoppingTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => lifetimeEvents)
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        clients.Add(client);

        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        IConsumer consumer = Substitute.For<IConsumer>();
        consumer.Name.Returns("consumer");
        consumer.Client.Returns(client);
        consumers.Add(consumer);

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        IProducer producer = Substitute.For<IProducer>();
        producer.Name.Returns("producer");
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("test"));
        producers.Add(producer);

        BrokerClientsConnectorService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        appStoppingTokenSource.Cancel();
        await service.StoppedAsync(CancellationToken.None);

        await consumer.Received(1).StopAsync();
        await client.Received(1).DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task StoppedAsync_ShouldWaitForConsumersAndDisconnectOnlyOnce_WhenCalledConcurrently()
    {
        TaskCompletionSource stoppingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stoppingCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disconnectStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disconnectCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenSource appStoppingTokenSource = new();
        IHostApplicationLifetime? lifetimeEvents = Substitute.For<IHostApplicationLifetime>();
        lifetimeEvents.ApplicationStopping.Returns(appStoppingTokenSource.Token);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddTransient(_ => lifetimeEvents)
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());

        BrokerClientCollection clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Name.Returns("client");
        client.DisconnectAsync().Returns(_ =>
        {
            disconnectStarted.TrySetResult();
            return new ValueTask(disconnectCompletion.Task);
        });
        clients.Add(client);

        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        IConsumer consumer = Substitute.For<IConsumer>();
        consumer.Name.Returns("consumer");
        consumer.Client.Returns(client);
        consumer.StopAsync().Returns(_ =>
        {
            stoppingStarted.TrySetResult();
            return new ValueTask(stoppingCompletion.Task);
        });
        consumers.Add(consumer);

        IHostedLifecycleService service = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        await service.StartAsync(CancellationToken.None);

        appStoppingTokenSource.Cancel();
        try
        {
            await stoppingStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Task stoppedTask1 = service.StoppedAsync(CancellationToken.None);
            Task stoppedTask2 = service.StoppedAsync(new CancellationToken(true));

            stoppedTask1.IsCompleted.ShouldBeFalse();
            stoppedTask2.ShouldBeSameAs(stoppedTask1);
            await client.Received(0).DisconnectAsync();

            stoppingCompletion.SetResult();
            await disconnectStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            stoppedTask1.IsCompleted.ShouldBeFalse();
            stoppedTask2.IsCompleted.ShouldBeFalse();
            service.StoppedAsync(CancellationToken.None).ShouldBeSameAs(stoppedTask1);

            disconnectCompletion.SetResult();
            await Task.WhenAll(stoppedTask1, stoppedTask2).WaitAsync(TimeSpan.FromSeconds(10));
            await service.StoppedAsync(CancellationToken.None);
        }
        finally
        {
            stoppingCompletion.TrySetResult();
            disconnectCompletion.TrySetResult();
        }

        await consumer.Received(1).StopAsync();
        await client.Received(1).DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task StoppedAsync_ShouldPropagateDisconnectFailureToAllCalls()
    {
        IBrokerClientsConnector connector = Substitute.For<IBrokerClientsConnector>();
        TaskCompletionSource disconnectCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connector.DisconnectAsync().Returns(_ => new ValueTask(disconnectCompletion.Task));
        BrokerClientsConnectorService service = new(
            new BrokerClientConnectionOptions(),
            Substitute.For<IHostApplicationLifetime>(),
            connector);

        Task stoppedTask1 = service.StoppedAsync(CancellationToken.None);
        Task stoppedTask2 = service.StoppedAsync(CancellationToken.None);
        InvalidOperationException exception = new("Disconnect failed");
        disconnectCompletion.SetException(exception);

        (await Should.ThrowAsync<InvalidOperationException>(stoppedTask1)).ShouldBeSameAs(exception);
        (await Should.ThrowAsync<InvalidOperationException>(stoppedTask2)).ShouldBeSameAs(exception);
        (await Should.ThrowAsync<InvalidOperationException>(() => service.StoppedAsync(CancellationToken.None))).ShouldBeSameAs(exception);
        await connector.Received(1).DisconnectAsync();
    }
}
