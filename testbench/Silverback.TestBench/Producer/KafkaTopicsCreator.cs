// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Kafka;
using Silverback.TestBench.ViewModel;

namespace Silverback.TestBench.Producer;

public class KafkaTopicsCreator
{
    private readonly IConfluentAdminClientFactory _adminClientFactory;

    private readonly MainViewModel _mainViewModel;

    private readonly ILogger<KafkaTopicsCreator> _logger;

    public KafkaTopicsCreator(IConfluentAdminClientFactory adminClientFactory, MainViewModel mainViewModel, ILogger<KafkaTopicsCreator> logger)
    {
        _adminClientFactory = adminClientFactory;
        _mainViewModel = mainViewModel;
        _logger = logger;
    }

    public async Task RecreateAllTopicsAsync()
    {
        IAdminClient adminClient = _adminClientFactory.GetClient(
            configuration => configuration
                .WithBootstrapServers("PLAINTEXT://localhost:19092,PLAINTEXT://localhost:29092"));

        Stopwatch stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Deleting Kafka topics");
        await TryDeleteTopicsAsync(adminClient);
        _logger.LogInformation("Kafka topics deleted in {Elapsed}", stopwatch.Elapsed);

        stopwatch.Restart();

        _logger.LogInformation("Creating Kafka topics");
        await TryCreateTopicsAsync(adminClient);
        _logger.LogInformation("Kafka topics created in {Elapsed}", stopwatch.Elapsed);
    }

    private async Task TryCreateTopicsAsync(IAdminClient adminClient, int tryCount = 1)
    {
        try
        {
            await adminClient.CreateTopicsAsync(
                _mainViewModel.KafkaTopics.Select(
                    topic =>
                        new TopicSpecification
                        {
                            Name = topic.TopicName,
                            NumPartitions = topic.PartitionsCount
                        }));
        }
        catch (CreateTopicsException)
        {
            if (tryCount > 10)
                throw;

            await Task.Delay(TimeSpan.FromSeconds(1 + tryCount));
            await TryCreateTopicsAsync(adminClient, tryCount + 1);
        }
    }

    private async Task TryDeleteTopicsAsync(IAdminClient adminClient)
    {
        try
        {
            await adminClient.DeleteTopicsAsync(_mainViewModel.KafkaTopics.Select(topic => topic.TopicName));
        }
        catch (DeleteTopicsException)
        {
            // Ignore
        }
    }
}
