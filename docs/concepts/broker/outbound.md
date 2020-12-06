---
uid: outbound
---

# Outbound Endpoint

An outbound endpoint is used to configure silverback to automatically relay the integration messages that ate published to the internal bus to the message broker. Multiple outbound endpoints can be configured and Silverback will route the messages according to their type or a custom routing logic.

The endpoint object identifies the topic/queue that is being connected and the client configuration, such the connection options. The endpoint object is therefore very specific and every broker type will define it's own implementation of [IProducerEndpoint](xref:Silverback.Messaging.IProducerEndpoint).

The options in the endpoint object are also used to tweak the Silverback behavior (e.g. the [serialization](xref:serialization)) and to enable additional features such as [chunking](xref:chunking), [encryption](xref:encryption), etc.

## Apache Kafka

The <xref:Silverback.Messaging.KafkaProducerEndpoint> is defined by
[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka) and is used to declare an outbound endpoint connected to Apache Kafka.

# [Fluent (preferred)](#tab/kafka-producer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<IIntegrationEvent>(endpoint => endpoint
                    .ProduceTo("order-events")
                    .EnableChunking(500000)
                    .ProduceToOutbox());
}
```
# [Legacy](#tab/kafka-producer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("order-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Chunk = new ChunkSettings
                    {
                        Size = 500000
                    },
                    Strategy = new OutboxProduceStrategy()
                });
}
```
***

> [!Note]
> For a more in-depth documentation about the Kafka client configuration refer also to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html).

## RabbitMQ

[Silverback.Integration.RabbitMQ](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ) is a bit more intricate and uses 2 different classes to specify an endpoint that connects to a queue (<xref:Silverback.Messaging.RabbitQueueProducerEndpoint>) or directly to an exchange (<xref:Silverback.Messaging.RabbitExchangeProducerEndpoint>).

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
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
                })
            .AddInbound(
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
                    });
}
```

> [!Note]
> For a more in-depth documentation about the RabbitMQ configuration refer to the [RabbitMQ tutorials and documentation](https://www.rabbitmq.com/getstarted.html).

## Transactional outbox strategy

The [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) purpose is to reliably update the database and publish the messages in the same atomic transaction. This is achieved storing the outbound messages into a temporary outbox table, whose changes are committed together with the other changes to the rest of the data.  

<figure>
	<a href="~/images/diagrams/outbound-outboxtable.png"><img src="~/images/diagrams/outbound-outboxtable.png"></a>
    <figcaption>Messages 1, 2 and 3 are stored in the outbox table and produced by a separate thread or process.</figcaption>
</figure>

When using entity framework the outbound messages are stored into a `DbSet` and are therefore implicitly saved in the same transaction used to save all other changes.

> [!Note]
> The [Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) package is also required and the `DbContext` must include a `DbSet` of <xref:Silverback.Database.Model.OutboxMessage>. See also the <xref:dbcontext>.

> [!Important]
> The current <xref:Silverback.Messaging.Outbound.TransactionalOutbox.OutboxWorker> cannot scale horizontally and starting multiple instances will cause the messages to be produced multiple times. In the following example a distributed lock (stored in the database) is used to ensure that only one instance is running and another one will _immediately_ take over when it stops (the `DbContext` must include a `DbSet` of <xref:Silverback.Database.Model.Lock> as well, see also the <xref:dbcontext>). 

# [Startup](#tab/outbox-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()

            // Setup the lock manager using the database
            // to handle the distributed locks.
            // If this line is omitted the OutboundWorker will still
            // work without locking. 
            .AddDbDistributedLockManager()

            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddOutboxDatabaseTable()
                .AddOutboxWorker())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [EndpointsConfigurator (fluent)](#tab/outbox-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<IIntegrationEvent>(
                    endpoint => endpoint
                        .ProduceTo("order-events")
                        .ProduceToOutbox()));
}
```
# [EndpointsConfigurator (legacy)](#tab/outbox-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("order-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Strategy = new OutboxProduceStrategy()
                });
}
```
***

### Custom outbox

You can easily use another kind of storage as outbox, simply creating your own <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxWriter> and <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxReader> implementations.

At the moment only a database table accessed using Entity Framework is supported as outbox, but a custom storage can be used implementing <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxWriter> and <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxReader>.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .AddDbDistributedLockManager()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddOutbox<MyCustomOutboxWriter, MyCustomOutboxReader()
                .AddOutboxWorker())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```


## Subscribing locally

The published messages that are routed to an outbound endpoint cannot be subscribed locally (within the same process), unless explicitly desired.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddDbDistributedLockManager()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>()
            .PublishOutboundMessagesToInternalBus();
    }
}
```

> [!Note]
> What said above is only partially true, as you can subscribe to the wrapped message (<xref:Silverback.Messaging.Messages.IOutboundEnvelope`1>) even without calling `PublishOutboundMessagesToInternalBus`.

## Producing the same message to multiple endpoints

An outbound route can point to multiple endpoints resulting in a broadcast to all endpoints.

<figure>
	<a href="~/images/diagrams/outbound-broadcast.png"><img src="~/images/diagrams/outbound-broadcast.png"></a>
    <figcaption>Messages 1, 2 and 3 are published to both topics simultaneously.</figcaption>
</figure>

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddOutbound<IIntegrationCommand>(
                new KafkaProducerEndpoint("topic-1")
                {
                    ...
                },
                new KafkaProducerEndpoint("topic-2")
                {
                    ...
                }));
    }
}
```

A message will also be routed to all outbound endpoint mapped to a type compatible with the message type. In the example below an `OrderCreatedMessage` (that inherits from `OrderMessage`) would be sent to both endpoints.

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddOutbound<OrderMessage>(
                new KafkaProducerEndpoint("topic-1")
                {
                    ...
                })
            .AddOutbound<OrderCreatedMessage>(
                new KafkaProducerEndpoint("topic-2")
                {
                    ...
                }));
    }
}
```

## Dynamic custom routing

By default Silverback routes the messages according to their type and the static configuration defined at startup. In some cases you may need more flexibility, being able to apply your own routing rules. In such cases it is possible to implement a fully customized router.

<figure>
	<a href="~/images/diagrams/outbound-customrouting.png"><img src="~/images/diagrams/outbound-customrouting.png"></a>
    <figcaption>The messages are dynamically routed to the appropriate endpoint.</figcaption>
</figure>

In the following example a custom router is used to route the messages according to their priority (a copy is also sent to a catch-all topic).

# [Startup](#tab/genericrouter-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [EndpointsConfigurator](#tab/genericrouter-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IPrioritizedCommand>(
                (message, _, endpointsDictionary) => 
                    new []
                    {
                        endpointsDictionary[message.Priority.ToString()],
                        endpointsDictionary["all"]
                    },
                new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                {
                    { "low", endpoint => endpoint.ProduceTo("low-priority") },
                    { "normal", endpoint => endpoint.ProduceTo("normal-priority") },
                    { "high", endpoint => endpoint.ProduceTo("high-priority") },
                    { "all", endpoint => endpoint.ProduceTo("all") }
                });
}
```
***

Alternatively, an actual router class can also be created to encapsulate the routing logic.

# [Router](#tab/router)
```csharp
public class PrioritizedRouter : OutboundRouter<IPrioritizedCommand>
{
    private static readonly IProducerEndpoint HighPriorityEndpoint =
        new KafkaProducerEndpoint("high-priority")
        {
            ...
        };
    private static readonly IProducerEndpoint NormalPriorityEndpoint =
        new KafkaProducerEndpoint("normal-priority")
        {
            ...
        };
    private static readonly IProducerEndpoint LowPriorityEndpoint =
        new KafkaProducerEndpoint("low-priority")
        {
            ...
        };
    private static readonly IProducerEndpoint AllMessagesEndpoint =
        new KafkaProducerEndpoint("all")
        {
            ...
        };

    public override IEnumerable<IProducerEndpoint> Endpoints
    {
        get
        {
            yield return AllMessagesEndpoint;
            yield return LowPriorityEndpoint;
            yield return NormalPriorityEndpoint;
            yield return HighPriorityEndpoint;
        }
    }

    public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
        IPrioritizedCommand message,
        MessageHeaderCollection headers)
    {
        yield return AllMessagesEndpoint;
        
        switch (message.Priority)
        {
            case MessagePriority.Low:
                yield return LowPriorityEndpoint;
                break;
            case MessagePriority.High:
                yield return HighPriorityEndpoint;
                break;
            default:
                yield return NormalPriorityEndpoint;
                break;
        }
    }
}
```
# [Startup](#tab/router-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>()
            .AddSingletonOutboundRouter<PrioritizedRouter>();
    }
}
```
# [EndpointsConfigurator](#tab/router-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder.AddOutbound<IPrioritizedCommand, PrioritizedRouter>();
}
```
***
