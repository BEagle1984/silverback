# Silverback Samples

This solution contains a set of samples based on Silverback.

## Prerequisites

Start the docker compose file in the repository root.

```bash
cd ..
docker-compose up -d 
```

## Kafka

### Basic

Produce and consume basic messages.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/Basic.Producer/.`
    1. `dotnet run --project ./samples/Kafka/Basic.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Batch Processing

Produce and consume in batch.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/Batch.Producer/.`
    1. `dotnet run --project ./samples/Kafka/Batch.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Batch Processing with Tombstones

Produce and consume in batch and handle tombstones.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/BatchWithTombstone.Producer/.`
    1. `dotnet run --project ./samples/Kafka/BatchWithTombstone.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Binary Files Streaming

Binary file streaming through Kafka.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/BinaryFileStreaming.Producer/.`
    1. `dotnet run --project ./samples/Kafka/BinaryFileStreaming.Consumer/.`
1. Browse the producer Swagger UI to fire the sample requests
    1. http://localhost:10001/swagger
1. The consumed files will be saved in `samples/temp`

### Catch-Up

Load an in-memory state from a topic at startup and get notified once it's loaded.

1. Run the producer, which produces a bunch of messages and exits
    1. `dotnet run --project ./samples/Kafka/CatchUp.Producer/.`
1. Run the consumer
    1. `dotnet run --project ./samples/Kafka/CatchUp.Consumer/.`
1. Observe the console output: the consumer logs once the catalog is loaded
1. Run the producer again (in another terminal) to see the catalog kept up to date, or restart the consumer to load it again from
   the beginning of the topic

### Avro

Produce and consume messages serialized as Avro using the schema registry.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/Avro.Producer/.`
    1. `dotnet run --project ./samples/Kafka/Avro.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Transactional producer

Produce messages leveraging Kafka transactions

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/TransactionalProducer.Producer/.`
    1. `dotnet run --project ./samples/Kafka/TransactionalProducer.Consumer/.`
1. Observe the console output while the messages are produced and consumed

## MQTT

### Basic

Produce and consume basic messages.

1. Run the two applications
    1. `dotnet run --project ./samples/MQTT/Basic.Producer/.`
    1. `dotnet run --project ./samples/MQTT/Basic.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Basic but using protocol version 3.1.0

Produce and consume basic messages.

1. Run the two applications
    1. `dotnet run --project ./samples/MQTT/Basic.ProducerV3/.`
    1. `dotnet run --project ./samples/MQTT/Basic.ConsumerV3/.`
1. Observe the console output while the messages are produced and consumed

### Binary Files Streaming

Binary file streaming over MQTT.

1. Run the two applications
    1. `dotnet run --project ./samples/MQTT/BinaryFileStreaming.Producer/.`
    1. `dotnet run --project ./samples/MQTT/BinaryFileStreaming.Consumer/.`
1. Browse the producer Swagger UI to fire the sample requests
    1. http://localhost:10001/swagger
1. The consumed files will be saved in `samples/temp` 
