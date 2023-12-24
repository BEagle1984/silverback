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

Process consumed messages in batch.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/Batch.Producer/.`
    1. `dotnet run --project ./samples/Kafka/Batch.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Binary Files Streaming

Binary file streaming through Kafka.

1. Run the two applications
    1. `dotnet run --project ./samples/Kafka/BinaryFileStreaming.Producer/.`
    1. `dotnet run --project ./samples/Kafka/BinaryFileStreaming.Consumer/.`
1. Browse the producer Swagger UI to fire the sample requests
    1. http://localhost:10001/swagger
1. The consumed files will be saved in `samples/temp`

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
