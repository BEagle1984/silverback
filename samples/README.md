# Silverback Samples

This solution contains a set of samples based on Silverback.

## Prerequisites

Start the docker compose file in the repository root.

```bash
cd ..
docker-compose up -d 
```

## Kafka

### Binary Files Streaming

Demonstrates how to stream a binary file through Kafka.

1. Run the two applications
    1. `dotnet run -p ./samples/Kafka/BinaryFileStreaming.Producer/.` 
    1. `dotnet run -p ./samples/Kafka/BinaryFileStreaming.Consumer/.`
1. Browse the producer Swagger UI to fire the sample requests
    1. http://localhost:10001/swagger
1. The consumed files will be saved in `samples/temp` 
