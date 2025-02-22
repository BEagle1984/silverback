using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Samples.Kafka.Avro.Producer;

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
            "samples-avro-value",
            new Schema(AvroMessage._SCHEMA.ToString(), null, SchemaType.Avro, null, null));

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
                new AvroMessage
                {
                    number = number.ToString(CultureInfo.InvariantCulture)
                });

            _logger.LogInformation("Produced {Number}", number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {Number}", number);
        }
    }
}
