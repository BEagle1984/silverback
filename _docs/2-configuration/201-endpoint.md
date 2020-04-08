---
title:  Endpoint
permalink: /docs/configuration/endpoint
---

The endpoint object contains all information that identify the topic/queue that is being connected and all the configurations. The endpoint object is therefore very specific and every broker type will define it's own implementation of `IEndpoint`.

## Kafka

`Silverback.Integration.Kafka` uses 2 different classes to specify inbound and outbound endpoints configuration.

For a more in-depth documentation about the Kafka configuration refer to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html).

### KafkaProducerEndpoint

Used for outbound endpoints, exposes the following properties:

Property | Description
:-- | :--
`Name` | The name of the topic. This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. See [Serialization]({{ site.baseurl }}/docs/advanced/serialization) for details.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Chunk` | Enable chunking to efficiently deal with large messages. See [Chunking]({{ site.baseurl }}/docs/advanced/chunking) for details.
`Configuration` | An instance of `KafkaProducerConfig`, that's just an extension of `Confluent.Kafka.ProducerConfig`.
`Configuration.BootstrapServers`, ...| All properties inherited from `Confluent.Kafka.ProducerConfig`. See [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.ProducerConfig.html) for details.
`Configuration.ThrowIfNotAcknowledged` | When set to `true` an exception will be thrown in the producer if no acknowledge is received by the broker (`PersistenceStatus.PossiblyPersisted`). The default is `true`.

```csharp
new KafkaProducerEndpoint("basket-events")
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
`Names` | The name of the topics to be consumed (yes, you can subscribe multiple topics at once). This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to serialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Configuration` | An instance of `KafkaConsumerConfig`, that's just an extension of `Confluent.Kafka.ConsumerConfig`.
`Configuration.BootstrapServers`, `Configuration.GroupId`, ...| All properties inherited from `Confluent.Kafka.ConsumerConfig`. See [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.ConsumerConfig.html) for details.
`Configuration.CommitOffsetEach` | When auto-commit is disable, defines the number of message processed before committing the offset to the server. The most reliable level is 1 but it reduces throughput.
`EnableAutoRecovery` | When set to `true` the consumer will be automatically restarted if a `KafkaException` is thrown while polling/consuming. The default is `true`.

```csharp
new KafkaConsumerEndpoint(
    "order-events", 
    "inventory-events")
{
    Configuration = new KafkaConsumerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092",
        GroupId = "my-consumer",
        AutoOffsetReset = AutoOffsetResetType.Earliest
    }
}
```

You can decide whether to use one consumer per topic or subscribe multiple topics with the same consumer (passing multiple topic names in the endpoint constructor, as shown in the example above). There are advantages and disadvantages of both solutions and the best choice really depends on your specific requirements, the amount of messages being produced, etc. Anyway the main difference is that when subscribing multiple topics you will still consume one message after the other but they will simply be interleaved (this may or may not be an issue, it depends) and on the other hand each consumer will use some resources, so creating multiple consumers will result in a bigger overhead.
{: .notice--note}

## RabbitMQ

`Silverback.Integration.RabbitMQ` is a bit more intricated and uses 4 different classes to specify inbound and outbound endpoints configuration.

For a more in-depth documentation about the RabbitMQ configuration refer to the [RabbitMQ tutorials and documentation](https://www.rabbitmq.com/getstarted.html).

### RabbitQueueProducerEndpoint

Used for outbound endpoints that produce directly to a queue, exposes the following properties:

Property | Description
:-- | :--
`Name` | The name of the queue. This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. See [Serialization]({{ site.baseurl }}/docs/advanced/serialization) for details.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Chunk` | Enable chunking to efficiently deal with large messages. See [Chunking]({{ site.baseurl }}/docs/advanced/chunking) for details.
`Connection` | An instance of `RabbitConnectionConfig`. It exposes the properties necessary to setup the connection with RabbitMQ.
`Connection.HostName`, ...| All properties exposed by the `RabbitMQ.Client.ConnectionFactory`. See [RabbitMQ .NET/C# Client API Guide](https://www.rabbitmq.com/dotnet-api-guide.html) for details.
`Queue` | An instance of `RabbitQueueConfig` that specifies the queue configuration.
`Queue.IsDurable` | Specifies whether the queue will survive a broker restart. The default is `true`.
`Queue.IsAutoDeleteEnabled` | Specifies whether the queue will be automatically deleted when the last consumer unsubscribes. The default is `false`.
`Queue.IsExclusive` | Specifies whether the queue is used by only one connection and will be deleted when that connection closes. The default is `false`.
`Queue.Arguments` | The optional arguments dictionary used by plugins and broker-specific features to configure values such as message TTL, queue length limit, etc.
`ConfirmationTimeout` | The maximum amount of time to wait for the message produce to be acknowledge before considering it failed. Set it to <c>null</c> to proceed without waiting for a positive or negative acknowledgment. The default is a quite conservative `5 seconds`.

```csharp
new RabbitQueueProducerEndpoint("inventory-commands-queue")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}
```

### RabbitExchangeProducerEndpoint

Used for outbound endpoints that produce to an exchange, exposes the following properties:

Property | Description
:-- | :--
`Name` | The name of the exchange. This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. See [Serialization]({{ site.baseurl }}/docs/advanced/serialization) for details.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Chunk` | Enable chunking to efficiently deal with large messages. See [Chunking]({{ site.baseurl }}/docs/advanced/chunking) for details.
`Connection` | An instance of `RabbitConnectionConfig`. It exposes the properties necessary to setup the connection with RabbitMQ.
`Connection.HostName`, ...| All properties exposed by the `RabbitMQ.Client.ConnectionFactory`. See [RabbitMQ .NET/C# Client API Guide](https://www.rabbitmq.com/dotnet-api-guide.html) for details.
`Exchange` | An instance of `RabbitExchangeConfig` that specifies the exchange configuration.
`Exchange.ExchangeType` | The exchange type. It should match with one of the constants declared in the `RabbitMQ.Client.ExchangeType` static class.
`Exchange.IsDurable` | Specifies whether the queue will survive a broker restart. The default is `true`.
`Exchange.IsAutoDeleteEnabled` | Specifies whether the queue will be automatically deleted when the last consumer unsubscribes. The default is `false`.
`Exchange.Arguments` | The optional arguments dictionary used by plugins and broker-specific features to configure values such as message TTL, queue length limit, etc.
`ConfirmationTimeout` | The maximum amount of time to wait for the message produce to be acknowledge before considering it failed. Set it to <c>null</c> to proceed without waiting for a positive or negative acknowledgment. The default is a quite conservative `5 seconds`.

```csharp
new RabbitExchangeProducerEndpoint("order-events")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Exchange = new RabbitExchangeConfig
    {
        IsDurable = true,
        IsAutoDeleteEnabled = false,
        ExchangeType = ExchangeType.Fanout
    }
}   
```

### RabbitQueueConsumerEndpoint

Used for inbound endpoints that consume directly from a queue, exposes the following properties:

Property | Description
:-- | :--
`Name` | The name of the queue. This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. See [Serialization]({{ site.baseurl }}/docs/advanced/serialization) for details.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Connection` | An instance of `RabbitConnectionConfig`. It exposes the properties necessary to setup the connection with RabbitMQ.
`Connection.HostName`, ...| All properties exposed by the `RabbitMQ.Client.ConnectionFactory`. See [RabbitMQ .NET/C# Client API Guide](https://www.rabbitmq.com/dotnet-api-guide.html) for details.
`Queue` | An instance of `RabbitQueueConfig` that specifies the queue configuration.
`Queue.IsDurable` | Specifies whether the queue will survive a broker restart. The default is `true`.
`Queue.IsAutoDeleteEnabled` | Specifies whether the queue will be automatically deleted when the last consumer unsubscribes. The default is `false`.
`Queue.IsExclusive` | Specifies whether the queue is used by only one connection and will be deleted when that connection closes. The default is `false`.
`Queue.Arguments` | The optional arguments dictionary used by plugins and broker-specific features to configure values such as message TTL, queue length limit, etc.
`AcknowledgeEach` | Defines the number of message processed before sending the acknowledgment to the server. The most reliable level is 1 but it reduces throughput.
`PrefetchSize` | Defines the QoS prefetch size parameter for the consumer. See [RabbitMQ Consumer Prefetch](https://www.rabbitmq.com/consumer-prefetch.html) documentation for details.
`PrefetchCount` | Defines the QoS prefetch count parameter for the consumer. See [RabbitMQ Consumer Prefetch](https://www.rabbitmq.com/consumer-prefetch.html) documentation for details.

```csharp
new RabbitQueueConsumerEndpoint("inventory-commands-queue")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}
```

### RabbitExchangeConsumerEndpoint

Used for inbound endpoints that consume from an exchange, exposes the following properties:

Property | Description
:-- | :--
`Name` | The name of the exchange. This is set in the constructor.
`Serializer` | The `IMessageSerializer` to be used to deserialize the messages. The default is a `JsonMessageSerializer` using UTF-8 encoding. See [Serialization]({{ site.baseurl }}/docs/advanced/serialization) for details.
`Encryption` | Enable end-to-end message encryption. See [Encryption]({{ site.baseurl }}/docs/advanced/encryption) for details.
`Connection` | An instance of `RabbitConnectionConfig`. It exposes the properties necessary to setup the connection with RabbitMQ.
`Connection.HostName`, ...| All properties exposed by the `RabbitMQ.Client.ConnectionFactory`. See [RabbitMQ .NET/C# Client API Guide](https://www.rabbitmq.com/dotnet-api-guide.html) for details.
`Exchange` | An instance of `RabbitExchangeConfig` that specifies the exchange configuration.
`Exchange.ExchangeType` | The exchange type. It should match with one of the constants declared in the `RabbitMQ.Client.ExchangeType` static class.
`Exchange.IsDurable` | Specifies whether the queue will survive a broker restart. The default is `true`.
`Exchange.IsAutoDeleteEnabled` | Specifies whether the queue will be automatically deleted when the last consumer unsubscribes. The default is `false`.
`Exchange.Arguments` | The optional arguments dictionary used by plugins and broker-specific features to configure values such as message TTL, queue length limit, etc.
`QueueName` | The desired queue name. If null or empty a random name will be generated by RabbitMQ. (A queue is always necessary to consume.)
`Queue` | An instance of `RabbitQueueConfig` that specifies the queue configuration.
`Queue.IsDurable` | Specifies whether the queue will survive a broker restart. The default is `true`.
`Queue.IsAutoDeleteEnabled` | Specifies whether the queue will be automatically deleted when the last consumer unsubscribes. The default is `false`.
`Queue.IsExclusive` | Specifies whether the queue is used by only one connection and will be deleted when that connection closes. The default is `false`.
`Queue.Arguments` | The optional arguments dictionary used by plugins and broker-specific features to configure values such as message TTL, queue length limit, etc.
`RoutingKey` | The routing key (aka binding key) to be used to bind with the exchange.
`ConfirmationTimeout` | The maximum amount of time to wait for the message produce to be acknowledge before considering it failed. Set it to <c>null</c> to proceed without waiting for a positive or negative acknowledgment. The default is a quite conservative `5 seconds`.
`AcknowledgeEach` | Defines the number of message processed before sending the acknowledgment to the server. The most reliable level is 1 but it reduces throughput.
`PrefetchSize` | Defines the QoS prefetch size parameter for the consumer. See [RabbitMQ Consumer Prefetch](https://www.rabbitmq.com/consumer-prefetch.html) documentation for details.
`PrefetchCount` | Defines the QoS prefetch count parameter for the consumer. See [RabbitMQ Consumer Prefetch](https://www.rabbitmq.com/consumer-prefetch.html) documentation for details.

```csharp
new RabbitExchangeConsumerEndpoint("order-events")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Exchange = new RabbitExchangeConfig
    {
        IsDurable = true,
        IsAutoDeleteEnabled = false,
        ExchangeType = ExchangeType.Fanout
    },
    QueueName = "my-consumer-group",
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}   
```