---
uid: sample-kafka-json-schema-registry
---

# Kafka - JSON with Schema Registry

This sample implements a producer and consumer which take advantage of the schema registry and serialize the messages as JSON, validated against a JSON schema.

## What This Sample Shows

- Enabling the Confluent schema registry integration with `AddConfluentSchemaRegistry`.
- Defining the JSON schema alongside the message class (as a constant excluded from serialization via `[JsonIgnore]`).
- Registering the schema for the `samples-json-schema-registry-value` subject at startup through the `IConfluentSchemaRegistryClientFactory`.
- Serializing with `SerializeAsJsonUsingSchemaRegistry`, disabling the automatic schema registration and using the latest registered version.
- Deserializing with `DeserializeJsonUsingSchemaRegistry`.

## How to Run

Start the infrastructure as described in <xref:samples> (the schema registry is required, listening on `localhost:8081`), then run the two applications from the repository root.

```bash
dotnet run --project ./samples/Kafka/JsonSchemaRegistry.Producer/.
dotnet run --project ./samples/Kafka/JsonSchemaRegistry.Consumer/.
```

Observe the console output while the messages are produced and consumed.

## Common

The message being exchanged and its JSON schema are defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/JsonSchemaRegistry.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/JsonSchemaRegistry.Common_

## Producer

The producer uses a hosted service to register the schema and then publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/JsonSchemaRegistry.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/JsonSchemaRegistry.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/JsonSchemaRegistry.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/JsonSchemaRegistry.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/JsonSchemaRegistry.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/JsonSchemaRegistry.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/JsonSchemaRegistry.Consumer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/JsonSchemaRegistry.Consumer_

## Additional Resources

- <xref:kafka-schema-registry-serializer>
- <xref:kafka-schema-registry-deserializer>
- <xref:serialization>
- <xref:deserialization>
- <xref:sample-kafka-avro> sample
