using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Samples.Kafka.JsonSchemaRegistry.Common;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory;

    private readonly IPublisher _publisher;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory,
        IPublisher publisher,
        ILogger<ProducerBackgroundService> logger)
    {
        _schemaRegistryClientFactory = schemaRegistryClientFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Push the Avro schema to the schema registry
        ISchemaRegistryClient schemaRegistry = _schemaRegistryClientFactory.GetClient(
            schemaRegistryConfigurationBuilder => schemaRegistryConfigurationBuilder
                .WithUrl("localhost:8081"));

        await schemaRegistry.RegisterSchemaAsync(
            "samples-json-schema-registry-value",
            new Schema(SampleMessage.Schema, null, SchemaType.Json, null, null));

        int number = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProduceMessageAsync(++number);

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProduceMessageAsync(int number)
    {
        try
        {
            await _publisher.PublishAsync(
                new SampleMessage
                {
                    Number = number
                });

            _logger.LogInformation("Produced {Number}", number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {Number}", number);
        }
    }
}
