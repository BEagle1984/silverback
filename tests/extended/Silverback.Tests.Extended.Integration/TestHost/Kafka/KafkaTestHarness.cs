// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

// Runs Silverback Kafka clients in the test process against the cluster. The new topics must be created through this harness,
// since it returns only once every broker knows them (see KafkaFixture.WaitUntilTopicsArePropagatedAsync).
public sealed class KafkaTestHarness : BrokerTestHarness
{
    private readonly List<Action<BrokerOptionsBuilder>> _brokerConfigurations = [];

    private readonly List<Action<KafkaClientsConfigurationBuilder>> _clientsConfigurations = [];

    private IAdminClient? _admin;

    private IProducer<Null, byte[]>? _producer;

    public KafkaTestHarness(string name, ITestOutputHelper output)
        : base(name, output)
    {
    }

    public IAdminClient Admin =>
        _admin ??= new AdminClientBuilder(new AdminClientConfig { BootstrapServers = KafkaFixture.BootstrapServers }).Build();

    // A native producer, to write the input of the test independently of Silverback
    public IProducer<Null, byte[]> Producer =>
        _producer ??= new ProducerBuilder<Null, byte[]>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                Acks = Acks.All
            }).Build();

    // Adds broker options, such as the schema registry or an offset store
    public void ConfigureBroker(Action<BrokerOptionsBuilder> configure)
    {
        EnsureHostIsNotBuilt();
        _brokerConfigurations.Add(configure);
    }

    // Adds the consumers and producers, which connect to the cluster
    public void AddClients(Action<KafkaClientsConfigurationBuilder> configure)
    {
        EnsureHostIsNotBuilt();
        _clientsConfigurations.Add(configure);
    }

    // Creates the topics with a single partition and waits until every broker knows them
    public async Task CreateTopicsAsync(params string[] topics)
    {
        await Admin.CreateTopicsAsync(topics.Select(topic => new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }));
        await KafkaFixture.WaitUntilTopicsArePropagatedAsync(topics, partitions: 1);
    }

    public Task<DeliveryResult<Null, byte[]>> ProduceJsonAsync<TMessage>(string topic, TMessage message) =>
        Producer.ProduceAsync(topic, new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(message) });

    public Task<DeliveryResult<Null, byte[]>> ProduceJsonAsync<TMessage>(TopicPartition partition, TMessage message) =>
        Producer.ProduceAsync(partition, new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(message) });

    // Builds the host, without starting it. StartHostAsync builds it when needed.
    public void Build() =>
        BuildHost(silverback => silverback
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();

                foreach (Action<BrokerOptionsBuilder> configure in _brokerConfigurations)
                {
                    configure(options);
                }
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(KafkaFixture.BootstrapServers);

                foreach (Action<KafkaClientsConfigurationBuilder> configure in _clientsConfigurations)
                {
                    configure(clients);
                }
            }));

    public override Task StartHostAsync()
    {
        if (!IsHostBuilt)
            Build();

        return base.StartHostAsync();
    }

    protected override Task DisposeResourcesAsync()
    {
        RunTeardownStep(() => _producer?.Dispose());
        RunTeardownStep(() => _admin?.Dispose());

        return Task.CompletedTask;
    }
}
