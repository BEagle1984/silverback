---
uid: message-broker
---

# Connecting to a Message Broker

To connect Silverback to a message broker we need a reference to `Silverback.Integration`, plus the concrete implementation (`Silverback.Integration.Kafka`  or `Silverback.Integration.RabbitMQ`). We can then add the broker to the DI and configure the connected endpoints. 

## Sample configuration

The following example is very simple and there are of course many more configurations and possibilities. Some more details are given in the dedicated <xref:endpoint> section.

The basic concepts:
* `WithConnectionToMessageBroker` registers the services necessary to connect to a message broker
* `AddKafka`, `AddRabbit`, `AddInMemoryBroker`, etc. register the message broker implementation(s)
* `AddEndpointsConfigurator` is used to outsource the endpoints configuration into a separate class implementing the `IEndpointsConfigurator` interface (of course multiple configurators can be registered)
* `AddInbound` is used to automatically relay the incoming messages to the internal bus and they can therefore be subscribed as seen in the previous chapters
* `AddOutbound` works the other way around and subscribes to the internal bus to forward the integration messages to the message broker

### Basic configuration

The following sample demonstrates how to setup some inbound and outbound endpoints against the built-in message brokers (Apache Kafka or RabbitMQ).

# [Apache Kafka - Startup](#tab/kafka-startup)
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
# [Apache Kafka - EndpointsConfigurator](#tab/kafka-configurator)
```csharp
private class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "order-service"
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("payment-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "order-service"
                    }
                })
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("order-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    }
                });
    }
}
```
# [RabbitMQ - Startup](#tab/rabbit-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddRabbit())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [RabbitMQ - EndpointsConfigurator](#tab/rabbit-configurator)
```csharp
private class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("basket-events")
                {
                    Connection = new RabbitConnectionConfig
                    {
                        HostName = "localhost",
                        UserName = "guest",
                        Password = "guest",
                    },
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Fanout
                    },
                    QueueName = "basket-events-order-service-queue",
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = true,
                        IsAutoDeleteEnabled = false
                    }
                })
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("payment-events")
                {
                    Connection = new RabbitConnectionConfig
                    {
                        HostName = "localhost",
                        UserName = "guest",
                        Password = "guest",
                    },
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Fanout
                    },
                    QueueName = "payment-events-order-service-queue",
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = true,
                        IsAutoDeleteEnabled = false
                    }
                })
            .AddOutbound<IIntegrationEvent>(
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
}
```
***

> [!Tip]
> All `IEndpointsConfigurator` implementations are registered as scoped services. Multiple implementations can be registerd to split the configuration and of course depenencies (such as `IOption` or a `DbContext`) can be injected to load the configuration variables.

> [!Important]
> Starting from version 3.0.0 the broker(s) will be connected and all consumers started automatically at startup, unless explicitely disabled (see the [Connection modes](#connection-modes) chapter for details).

### Inline endpoints configuration

The preferred and suggested way to configure the message broker endpoints is using the `IEndpointsConfiguration` but you can use `AddEndpoints` to specify them inline.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpoints(builder => builder
                .AddInbound(
                    new KafkaConsumerEndpoint("kafka-events")
                    {
                        ...
                    }));
    }
}

A third option would be to use `ConfigureEndpoints` on the `IBroker`/`IBrokerCollection` directly but that approach should be used only if the automatic connection is disabled (see the [Connection modes](#connection-modes) chapter for details).

### Multiple brokers

It is possible to use multiple message broker implementation together in the same application. The following sample demonstrates how to consume from both Apache Kafka and RabbitMQ.

# [Startup](#tab/multiple-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddRabbit())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [EndpointsConfigurator](#tab/multiple-kafka-configurator)
```csharp
private class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("kafka-events")
                {
                    ...
                })
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("rabbit-events")
                {
                    ...
                });
    }
}
```
***

### Using assembly scanning

You can of course use the assembly scanning capabilities of your Dependency Injection framework (e.g. [Autofac](https://autofac.org/)) to register all the `IEndpointsConfigurator`.

Here an example using Autofac:

```csharp
public class EndpointsConfiguratorsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(t => t.IsAssignableTo<IEndpointsConfigurator>())
            .AsImplementedInterfaces();
    }
}
```

## Connection modes

You may not want to connect your broker immediately. In the following example is shown how to postpone the automatic connection after the application startup.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .WithConnectionOptions(new BrokerConnectionOptions
                {
                    Mode = BrokerConnectionMode.AfterStartup,
                    RetryInterval = TimeSpan.FromMinutes(5)

                }))
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```

But it's also possible to completely disable the automatic connection and manually perform it.

# [Startup](#tab/noautoconnect-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .WithConnectionOptions(new BrokerConnectionOptions
                {
                    Mode = BrokerConnectionMode.Manual
                }))
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [Service](#tab/noautoconnect-service)
```csharp
public class BrokerConnectionService
{
    private readonly IBroker _broker;

    public BrokerConnectionService(IBroker broker)
    {
        _broker = broker;
    }

    public void Connect()
    {
        broker.Connect();
    }
}
```
***

> [!Tip]
> See the <xref:Silverback.Messaging.Configuration.BrokerConnectionOptions> documentation for details about the different options.

> [!Note]
> Use `IBrokerCollection` instead of `IBroker` when multiple broker implementations are used.

> [!Important]
If your application is not running using an `IHost` (`GenericHost` or `WebHost`, like in a normal ASP.NET Core application) you always need to manually connect it as shown in the second example above.

## Graceful shutdown

It is important to properly close the consumers using the `Disconnect` method before exiting. The offsets have to be committed and the broker has to be notified (it will then proceed to reassign the partitions as needed). Starting from version 3.0.0 this is done automatically (if your application is running using an `IHost` (`GenericHost` or `WebHost`, like in a normal ASP.NET Core application)).

## Health Monitoring

The `Silverback.Integration.HealthChecks` package contains some extensions for `Microsoft.Extensions.Diagnostics.HealthChecks` that can be used to monitor the connection to the message broker.

Currently two checks exists:
* `AddOutboundEndpointsCheck`: Adds an health check that sends a ping message to all the outbound endpoints.
* `AddOutboundQueueCheck`: Adds an health check that monitors the outbound queue (outbox table), verifying that the messages are being processed.
* `AddConsumersCheck`: Adds an health check that verifies that all consumers are connected.

The usage is very simple, you just need to configure the checks in the Startup.cs, as shown in the following example.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddOutboundEndpointsCheck()
            .AddOutboundQueueCheck()
            .AddConsumersCheck();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");
    }
}
```

## Consumer management API

The consumer exposes some information and statistics that can be used to programmatically check the consumer status (see <xref:Silverback.Messaging.Broker.IConsumer#Silverback_Messaging_Broker_IConsumer_StatusInfo>). A consumer can also be connected and disconnected at will.

The following example shows a sample service that is used to monitor the total number of consumed message and restart the faulted consumers (the consumers get disconnected when an unhandled exception is thrown while processing the consumed message).

```csharp
public class ConsumerManagementService
{
    private readonly IBrokerCollection _brokers;

    public ConsumerManagementService(IBrokerCollection brokers)
    {
        _brokers = brokers;
    }

    public int GetTotalConsumedMessages()
    {
        int totalCount = 0;

        foreach (var broker in _brokers)
        {
            foreach (var consumer in broker.Consumers)
            {
                totalCount += consumer.StatusInfo.ConsumedMessagesCount;
            }
        }
    }

    public void RestartDisconnectedConsumers()
    {
        foreach (var broker in _brokers)
        {
            if (!broker.IsConnected)
                continue;

            foreach (var consumer in broker.Consumers)
            {
                if (consumer.StatusInfo.Status == ConsumerStatus.Disconnected)
                {
                    consumer.Connect();
                }
            }
        }
    }
}
```
