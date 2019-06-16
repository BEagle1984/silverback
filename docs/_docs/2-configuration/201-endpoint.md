---
title:  Endpoint
permalink: /docs/configuration/endpoint
---

The endpoint object contains all information that identify the topic/queue that is being connected and all the configurations. The endpoint object is therefore very specific and every broker type will define it's own implementation of `IEndpoint`.

## Kafka

Silverback.Integration.Kafka uses two different classes to specify inbound and outbound endpoints configuration.

### KafkaProducerEndpoint

Used for outbound endpoints, exposes the following properties:

Property | Description
:-- | :--
Name | The name of the topic. This is set in the constructor.
Serializer | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. (see also [Custom Serializer]({{ site.baseurl }}/docs/advanced/chunking))
Configuration | An instance of `KafkaProducerConfig`, that's just an extension of `Confluent.Kafka.ProducerConfig`.
Configuration.BootstrapServers, ...| All properties inherited from `Confluent.Kafka.ProducerConfig`. See [confluent-kafka-dotnet project](https://github.com/confluentinc/confluent-kafka-dotnet) for details.
Chunk | Enable chunking to efficiently deal with large messages. See [Chunking]({{ site.baseurl }}/docs/advanced/chunking) for details.

```c#
new KafkaProducerEndpoint("silverback-examples-events")
{
    Configuration = new KafkaProducerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092"
    },
    Chunk = ...
}
```

### KafkaConsumerEndpoint

Used for inbound endpoints, exposes the following properties:

Property | Description
:-- | :--
Names | The name of the topics to be consumed. This is set in the constructor.
Serializer | The `IMessageSerializer` to be used to serialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding.
Configuration | An instance of `KafkaConsumerConfig`, that's just an extension of `Confluent.Kafka.ConsumerConfig`.
Configuration.BootstrapServers, Configuration.EnableAutoCommit, ...| All properties inherited from `Confluent.Kafka.ConsumerConfig`. See [confluent-kafka-dotnet project](https://github.com/confluentinc/confluent-kafka-dotnet) for details.
Configuration.CommitOffsetEach | When auto-commit is disable, defines the number of message processed before committing the offset to the server. The most reliable level is 1 but it reduces throughput.

```c#
new KafkaConsumerEndpoint("silverback-examples-events", "silverback-examples-something")
{
    ConsumerThreads = 3,
    Configuration = new KafkaConsumerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092",
        ClientId = "ClientTest",
        GroupId = "advanced-silverback-consumer",
        EnableAutoCommit = false,
        AutoCommitIntervalMs = 5000,
        StatisticsIntervalMs = 60000,
        AutoOffsetReset = Confluent.Kafka.AutoOffsetResetType.Earliest
    }
}
```