// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

// Runs Silverback MQTT clients against the cluster. The cluster nodes replicate the subscriptions asynchronously and the proxy
// spreads the clients across the nodes, so a message published right after a subscription was acknowledged can be dropped. Each
// method connecting the Silverback clients or subscribing a test client therefore returns only once the subscriptions reached all
// the nodes, and the tests must publish only after these methods return.
public sealed class MqttTestHarness : BrokerTestHarness
{
    private const int PubRelType = 6;

    // The session cleanup is best effort, so that an unreachable broker doesn't delay the teardown for each client
    private static readonly TimeSpan SessionCleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly List<(string ClientId, Action<MqttClientConfigurationBuilder> Configure)> _clients = [];

    private readonly List<TestMqttClient> _testClients = [];

    private readonly NativeClientFactory _clientFactory = new();

    private DisconnectableTcpProxy? _proxy;

    private IBrokerClientsConnector? _connector;

    private MqttConsumer[] _consumers = [];

    private SubscriptionTracker[] _subscriptions = [];

    public MqttTestHarness(string name, ITestOutputHelper output)
        : base(name, output)
    {
    }

    // The MQTTnet client of the first Silverback client. It exists before the host is built, so that the test can add its event
    // handlers before or after the ones of Silverback (MQTTnet invokes the handlers in the order they were added).
    public IMqttClient NativeClient => _clientFactory.FirstClient;

    public IBrokerClientsConnector Connector => _connector ?? throw new InvalidOperationException("The harness isn't initialized yet.");

    // The consumer of the only Silverback client
    public MqttConsumer Consumer => _consumers.Single();

    public IReadOnlyList<MqttConsumer> Consumers => _consumers;

    // The number of times the consumer subscribed, including the resubscriptions after reconnecting
    public int SubscriptionsCount => _subscriptions.Single().Count;

    // Adds a Silverback client with the prefix as client id
    public void AddClient(Action<MqttClientConfigurationBuilder> configure) => AddClient(Prefix, configure);

    public void AddClient(string clientId, Action<MqttClientConfigurationBuilder> configure)
    {
        EnsureHostIsNotBuilt();
        _clients.Add((clientId, configure));
    }

    // Connects the Silverback clients through a proxy that can drop their connections, to simulate a connection loss
    public DisconnectableTcpProxy UseDisconnectableProxy()
    {
        EnsureHostIsNotBuilt();

        return _proxy ??= new DisconnectableTcpProxy();
    }

    // Builds the host and initializes the clients, without connecting them
    public async Task InitializeAsync()
    {
        BuildHost(
            silverback => silverback
                .WithConnectionToMessageBroker(options => options.AddMqtt())
                .AddMqttClients(clients =>
                {
                    if (_proxy != null)
                        clients.ConnectViaTcp("127.0.0.1", _proxy.Port);
                    else
                        clients.ConnectViaTcp(MqttFixture.BrokerHost);

                    foreach ((string clientId, Action<MqttClientConfigurationBuilder> configure) in _clients)
                    {
                        clients.AddClient(client =>
                        {
                            client.WithClientId(clientId);
                            configure(client);
                        });
                    }
                }),
            services => services.AddSingleton<IMqttNetClientFactory>(serviceProvider =>
            {
                _clientFactory.BindLogger(serviceProvider.GetRequiredService<IMqttNetLogger>());

                return _clientFactory;
            }));

        _connector = Services.GetRequiredService<IBrokerClientsConnector>();
        await _connector.InitializeAsync();

        // Track the subscriptions before connecting, since the clients subscribe right after the connection. A client without
        // consumer endpoints has nothing to wait for, so the connect methods don't wait for it to connect.
        _consumers = [.. Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>()];
        _subscriptions = [.. _consumers.Select(consumer => new SubscriptionTracker(consumer))];
    }

    // Connects the Silverback clients and waits until they subscribed again on all the nodes
    public async Task ConnectAsync()
    {
        int[] subscriptionsCounts = GetSubscriptionsCounts();
        await Connector.ConnectAsync().AsTask().WaitAsync(Timeout);
        await WaitUntilAllSubscribedAsync(subscriptionsCounts);
    }

    // Starts the host, which connects the Silverback clients, and waits until they subscribed on all the nodes
    public override async Task StartHostAsync()
    {
        int[] subscriptionsCounts = GetSubscriptionsCounts();
        await base.StartHostAsync();
        await WaitUntilAllSubscribedAsync(subscriptionsCounts);
    }

    // Disconnects the consumer, keeping its persistent session
    public Task DisconnectConsumerAsync() => Consumer.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

    // Connects the consumer again, after the test disconnected it, and waits until it subscribed again on all the nodes
    public async Task ReconnectAsync()
    {
        int subscriptionsCount = SubscriptionsCount;
        await Consumer.Client.ConnectAsync().AsTask().WaitAsync(Timeout);
        await WaitUntilSubscribedAsync(subscriptionsCount + 1);
    }

    // Waits until the consumer subscribed the specified number of times in total, for example after the client reconnected on
    // its own, and until its subscriptions reached all the nodes
    public Task WaitUntilSubscribedAsync(int count) => _subscriptions.Single().WaitUntilSubscribedAsync(count, Timeout);

    // Creates and connects a client that isn't managed by Silverback, with the prefix and the suffix as client id
    public async Task<TestMqttClient> CreateClientAsync(string suffix)
    {
        TestMqttClient client = new(new MqttClientFactory().CreateMqttClient(), Timeout);
        _testClients.Add(client);

        await client.Native.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(MqttFixture.BrokerHost)
            .WithClientId($"{Prefix}-{suffix}")
            .Build()).WaitAsync(Timeout);

        return client;
    }

    // Resumes the persistent session of the consumer, which the test disconnected, with a native client and returns the payloads the
    // broker replays because they were never acknowledged, and the number of PUBREL packets it sends again because their QoS 2
    // delivery was never completed. A sentinel published to the topic while the session is disconnected is queued after them (the
    // broker resends the unacknowledged deliveries and releases before the queued messages), so it marks the end of the replay. The
    // session ends when the native client disconnects.
    public async Task<SessionReplay> ResumeAndEndSessionAsync(string topic)
    {
        if (Consumer.Client.Status != ClientStatus.Disconnected)
            throw new InvalidOperationException("The consumer must be disconnected to resume its session.");

        string clientId = Consumer.Configuration.ClientId;
        byte[] sentinel = Encoding.UTF8.GetBytes("sentinel-" + Guid.NewGuid().ToString("N"));

        using (IMqttClient publisher = new MqttClientFactory().CreateMqttClient())
        {
            await publisher.ConnectAsync(CreateOptionsBuilder($"{Prefix}-sentinel").Build()).WaitAsync(Timeout);
            await publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(sentinel)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()).WaitAsync(Timeout);
            await publisher.DisconnectAsync().WaitAsync(Timeout);
        }

        ConcurrentQueue<byte[]> replayed = new();
        int releases = 0;
        TaskCompletionSource<bool> sentinelReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        client.InspectPacketAsync += args =>
        {
            if (args.Direction == MqttPacketFlowDirection.Inbound && args.Buffer.Length > 0 && args.Buffer[0] >> 4 == PubRelType)
                Interlocked.Increment(ref releases);

            return Task.CompletedTask;
        };
        client.ApplicationMessageReceivedAsync += args =>
        {
            byte[] payload = args.ApplicationMessage.Payload.ToArray();

            if (payload.AsSpan().SequenceEqual(sentinel))
                sentinelReceived.TrySetResult(true);
            else
                replayed.Enqueue(payload);

            return Task.CompletedTask;
        };

        MqttClientConnectResult result = await client.ConnectAsync(CreateOptionsBuilder(clientId)
            .WithCleanStart(false)
            .WithSessionExpiryInterval(0)
            .Build()).WaitAsync(Timeout);

        try
        {
            // Without a session, nothing is subscribed and the sentinel isn't delivered
            if (result.IsSessionPresent)
                await sentinelReceived.Task.WaitAsync(Timeout);
        }
        finally
        {
            await client.DisconnectAsync().WaitAsync(Timeout);
        }

        // The releases are sent before the sentinel, whose delivery was awaited
        return new SessionReplay(result.IsSessionPresent, [.. replayed], releases);
    }

    protected override async Task StopClientsAsync()
    {
        if (IsHostStarted)
            await base.StopClientsAsync();
        else if (_connector != null)
            await _connector.DisconnectAsync().AsTask().WaitAsync(Timeout);
    }

    protected override async Task DisposeResourcesAsync()
    {
        foreach (SubscriptionTracker subscription in _subscriptions)
        {
            RunTeardownStep(subscription.Dispose);
        }

        foreach (TestMqttClient client in _testClients)
        {
            await RunTeardownStepAsync(() => client.DisposeAsync().AsTask());
        }

        if (_proxy != null)
            await RunTeardownStepAsync(() => _proxy.DisposeAsync().AsTask().WaitAsync(Timeout));

        RunTeardownStep(_clientFactory.DisposeClients);

        // Delete the sessions last, so that no Silverback client can reconnect and recreate them
        foreach ((string clientId, _) in _clients)
        {
            await DeleteSessionAsync(clientId);
        }
    }

    private static MqttClientOptionsBuilder CreateOptionsBuilder(string clientId) =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(MqttFixture.BrokerHost)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(clientId);

    // Deletes the session of a Silverback client, including a persistent one configured without an expiry, by connecting with a
    // clean session that expires on disconnect
    private async Task DeleteSessionAsync(string clientId)
    {
        try
        {
            using IMqttClient client = new MqttClientFactory().CreateMqttClient();
            await client.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(clientId)
                .WithCleanSession()
                .WithSessionExpiryInterval(0)
                .Build()).WaitAsync(SessionCleanupTimeout);
            await client.DisconnectAsync().WaitAsync(SessionCleanupTimeout);
        }
        catch (Exception exception)
        {
            // The cleanup is best effort, the session expires anyway
            Output.WriteLine($"Could not delete the session of {clientId}: {exception.Message}");
        }
    }

    private int[] GetSubscriptionsCounts() => [.. _subscriptions.Select(subscription => subscription.Count)];

    // Waits for the next subscription of each consumer, so that connecting again after a disconnection also waits for the propagation
    private Task WaitUntilAllSubscribedAsync(int[] previousCounts) =>
        Task.WhenAll(_subscriptions.Select((subscription, index) => subscription.WaitUntilSubscribedAsync(previousCounts[index] + 1, Timeout)));

    // Hands the native client to the first Silverback client and creates new ones for the others (Silverback creates a single
    // native client per Silverback client). It isn't IDisposable, since the host would dispose it before the harness can.
    private sealed class NativeClientFactory : IMqttNetClientFactory
    {
        private readonly List<IMqttClient> _otherClients = [];

        private readonly ForwardingMqttNetLogger _logger = new();

        private bool _isFirstClientTaken;

        public NativeClientFactory()
        {
            FirstClient = new MqttClientFactory().CreateMqttClient(_logger);
        }

        public IMqttClient FirstClient { get; }

        // Logs the MQTTnet events through the logger that Silverback registers, as its default factory does, so that the MQTTnet
        // errors are logged as well
        public void BindLogger(IMqttNetLogger logger) => _logger.Bind(logger);

        public IMqttClient CreateClient()
        {
            lock (_otherClients)
            {
                if (!_isFirstClientTaken)
                {
                    _isFirstClientTaken = true;

                    return FirstClient;
                }

                IMqttClient client = new MqttClientFactory().CreateMqttClient(_logger);
                _otherClients.Add(client);

                return client;
            }
        }

        public void DisposeClients()
        {
            FirstClient.Dispose();

            foreach (IMqttClient client in _otherClients)
            {
                client.Dispose();
            }
        }
    }

    // The first native client is created before the host, which registers the logger, so it logs through this forwarder, bound
    // when the host creates the factory
    private sealed class ForwardingMqttNetLogger : IMqttNetLogger
    {
        private IMqttNetLogger? _target;

        public bool IsEnabled => _target?.IsEnabled ?? true;

        public void Bind(IMqttNetLogger target) => _target = target;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception) =>
            _target?.Publish(logLevel, source, message, parameters, exception);
    }
}
