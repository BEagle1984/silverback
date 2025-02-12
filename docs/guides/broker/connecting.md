---
uid: broker
---

# Connecting to a Message Broker

To connect Silverback to a message broker, you need to reference the appropriate integration package.

# [Kafka](#tab/kafka)
```poweshell
dotnet add package Silverback.Integration.Kafka
```
# [MQTT](#tab/mqtt)
```poweshell
dotnet add package Silverback.Integration.Mqtt
```
***

Once the package is referenced, add the broker to your Silverback configuration:

# [Kafka](#tab/kafka)
```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka());
```
# [MQTT](#tab/mqtt)
```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt());
```
***

You can also configure multiple brokers within the same application:

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddMqtt());
```

## Configuring Clients and Endpoints

The next step is configuring broker clients and endpoints for message consumption and production.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo("my-topic")))
        .AddConsumer(consumer => consumer
            .WithGroupId("consumer1")
            .AutoResetOffsetToEarliest()
            .Consume<MyOtherMessage>(endpoint => endpoint
                .ConsumeFrom("my-other-topic")
                .EnableBatchProcessing(100, TimeSpan.FromSeconds(5))
                .OnError(policy => policy.Retry(3).ThenSkip()))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient(client => client
            .WithClientId("my.client")
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo("messages/my")
                .WithAtLeastOnceQoS()
                .Retain()
                .IgnoreNoMatchingSubscribersError())
            .Consume(endpoint => endpoint
                .ConsumeFrom("messages/other")
                    .WithAtLeastOnceQoS()
                    .OnError(policy => policy.Skip()))
            .SendLastWillMessage<TestamentMessage>(lastWill => lastWill
                .SendMessage(new TestamentMessage(){ ... })
                .ProduceTo("testaments"))));
```
***

Since client configuration can become verbose, it’s best to separate it into a dedicated class using the <xref:Silverback.Messaging.Configuration.IBrokerClientsConfigurator> interface.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddBrokerClientsConfigurator<MyClientsConfigurator>();
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddBrokerClientsConfigurator<MyClientsConfigurator>();
```
***

# [Kafka](#tab/kafka)
```csharp
public class MyClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder.AddKafkaClients(clients => clients
            .WithBootstrapServers("PLAINTEXT://localhost:9092")
            .AddProducer(producer => producer
                .Produce<MyMessage>(endpoint => endpoint
                    .ProduceTo("my-topic")))
            .AddConsumer(consumer => consumer
                .WithGroupId("consumer1")
                .AutoResetOffsetToEarliest()
                .Consume<MyOtherMessage>(endpoint => endpoint
                    .ConsumeFrom("my-other-topic")
                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(5))
                    .OnError(policy => policy.Retry(3).ThenSkip()))));
    }
}
```
# [MQTT](#tab/mqtt)
```csharp
public class MyClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder.AddMqttClients(clients => clients
            .ConnectViaTcp("localhost")
            .AddClient(client => client
                .WithClientId("my.client")
                .Produce<MyMessage>(endpoint => endpoint
                    .ProduceTo("messages/my")
                    .WithAtLeastOnceQoS()
                    .Retain()
                    .IgnoreNoMatchingSubscribersError())
                .Consume(endpoint => endpoint
                    .ConsumeFrom("messages/other")
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.Skip()))
                .SendLastWillMessage<TestamentMessage>(lastWill => lastWill
                    .SendMessage(new TestamentMessage(){ ... })
                    .ProduceTo("testaments"))));
    }
}
```
***

> [!Tip]
> <xref:Silverback.Messaging.Configuration.IBrokerClientsConfigurator> implementations are registered as scoped services. You can register multiple implementations to split the configuration and inject dependencies (e.g., `IOptions` or `DbContext`) to load variables dynamically.

## Connection Modes

By default, Silverback connects to the message broker when the application starts. However, you can modify this behavior.

You can postpone the connection to after the application has started using `BrokerClientConnectionMode.AfterStartup`.

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .WithConnectionOptions(new BrokerClientConnectionOptions
        {
            Mode = BrokerClientConnectionMode.AfterStartup
        }));
```

> [!Tip]
> [Callbacks](xref:broker-callbacks) can be used to execute custom code when the connection is established.

You can also completely disable automatic connection and handle it manually when needed.

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .WithConnectionOptions(new BrokerClientConnectionOptions
        {
            Mode = BrokerClientConnectionMode.Manual
        }))
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo("my-topic")))
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("consumer1")
            .Consume<MyOtherMessage>(endpoint => endpoint
                .ConsumeFrom("my-other-topic"))));
;
```

In this case, you must manually connect the clients:

```csharp
public class MyService
{
    private readonly IBrokerClientCollection _clients;

    public MyService(IBrokerClientCollection clients)
    {
        _clients = clients;
    }

    public async ValueTask ConnectAll()
    {
        await _clients.ConnectAllAsync();
    }
    
    public async ValueTask ConnectConsumer()
    {
        await _clients["consumer1"].ConnectAsync();
    }
    
    public async ValueTask ConnectProducer()
    {
        await _clients["producer1"].ConnectAsync();
    }
}
```

> [!Tip]
> Assigning names to clients helps manage them individually.

> [!Important]
> If your application is not using an `IHost` (`GenericHost` or `WebHost`), you must connect the clients manually as shown above.

## Graceful shutdown

Silverback automatically disconnects consumers and producers gracefully when the application stops. This ensures consistency and is performed regardless of the connection mode.

> [!Important]
> If your application is not using an `IHost` (`GenericHost` or `WebHost`), you must take care of the clients' graceful shutdown manually.

## Consumers Management

You can monitor and control consumers programmatically.

The following example demonstrates how to monitor the total number of consumed messages, stop and start a consumer to temporarily pause it, and restart any consumers that have been disconnected due to an unhandled exception during message processing.

```csharp
public class ConsumerManagementService
{
    private readonly IConsumerCollection _consumers;

    public ConsumerManagementService(IConsumerCollection consumers)
    {
        _consumers = consumers;
    }

    public int GetTotalConsumedMessages()
    {
        int totalCount = 0;

        foreach (IConsumer in _consumers
        {
            totalCount += consumer.StatusInfo.ConsumedMessagesCount;
        }
        
        return totalCount;
    }
    
    public async ValueTask Start(string consumerName)
    {
        await _consumers[consumerName].StartAsync();
    }
    
    public async ValueTask Stop(string consumerName)
    {
        await _consumers[consumerName].StopAsync();
    }

    public async ValueTask RestartDisconnectedConsumers()
    {
        foreach (IConsumer consumer in _consumers)
        {
            if (consumer.Client.Status == ClientStatus.Disconnected)
            {
                await consumer.Client.ConnectAsync();
            }
        }
    }
}
```

> [!Tip]
> Assigning unique names to clients helps manage them programmatically.

## Additional Resources

* <xref:producing>
* <xref:consuming>
* <xref:samples>

