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

Produce and consume basic message.

1. Run the two applications
    1. `dotnet run -p ./samples/Kafka/Basic.Producer/.` 
    1. `dotnet run -p ./samples/Kafka/Basic.Consumer/.`
1. Observe the console output while the messages are produced and consumed

### Binary Files Streaming

Binary file streaming through Kafka.

1. Run the two applications
    1. `dotnet run -p ./samples/Kafka/BinaryFileStreaming.Producer/.` 
    1. `dotnet run -p ./samples/Kafka/BinaryFileStreaming.Consumer/.`
1. Browse the producer Swagger UI to fire the sample requests
    1. http://localhost:10001/swagger
1. The consumed files will be saved in `samples/temp` 
