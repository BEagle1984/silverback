---
uid: message-broker
---

# Connecting to a Message Broker

To connect Silverback to a message broker we need a reference to [Silverback.Integration](https://www.nuget.org/packages/Silverback.Integration), plus the concrete implementation ([Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka), [Silverback.Integration.RabbitMQ](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ), etc.). We can then add the broker to the DI and configure the connected endpoints. 

## Sample configuration

The following example is very basic and there are of course many more configurations and possibilities. Some more details are given in the dedicated <xref:outbound> and <xref:inbound> sections.

The basic concepts:
* `WithConnectionToMessageBroker` registers the services necessary to connect to a message broker
* `AddKafka`, `AddRabbit`, etc. register the message broker implementation(s)
* `AddEndpointsConfigurator` is used to outsource the endpoints configuration into a separate class implementing the <xref:Silverback.Messaging.Configuration.IEndpointsConfigurator> interface (of course multiple configurators can be registered)
* `AddInbound` is used to automatically relay the incoming messages to the internal bus and they can therefore be subscribed as seen in the previous chapters
* `AddOutbound` works the other way around and subscribes to the internal bus to forward the integration messages to the message broker

More complex and complete samples can be found in the <xref:samples> section.

### Basic configuration

The following sample demonstrates how to setup some inbound and outbound endpoints against the built-in message brokers (Apache Kafka or RabbitMQ).

#### Apache Kafka

# [Startup](#tab/kafka-startup)
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
# [EndpointsConfigurator](#tab/kafka-configurator)
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
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("basket-events")
                    .Configure(config =>
                    {
                        config.GroupId = "order-service";
                    }))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("basket-events")
                    .Configure(config =>
                    {
                        confing.GroupId = "order-service"
                    }))
                .AddOutbound<IIntegrationEvent>(endpoint => endpoint
                    .ProduceTo("order-events")));
}
```
***

#### RabbitMQ

# [Startup](#tab/rabbit-startup)
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
# [EndpointsConfigurator](#tab/rabbit-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
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
> All <xref:Silverback.Messaging.Configuration.IEndpointsConfigurator> implementations are registered as scoped services. Multiple implementations can be registered to split the configuration and of course dependencies (such as `IOption` or a `DbContext`) can be injected to load the configuration variables.

> [!Important]
> Starting from version 3.0.0 the broker(s) will be connected and all consumers started automatically at startup, unless explicitly disabled (see the [Connection modes](#connection-modes) chapter for details).

### Inline endpoints configuration

The preferred and suggested way to configure the message broker endpoints is using the <xref:Silverback.Messaging.Configuration.IEndpointsConfigurator> but you can use `AddEndpoints` (or `AddKafkaEndpoints` etc.) directly and configure everything inline.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(...)
                .AddOutbound<IIntegrationEvent>(...));
    }
}
```

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
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(...)
                .AddOutbound<IIntegrationEvent>(...))
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("rabbit-events")
                {
                    ...
                });
    }
}
```
***

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

    public async ConnectAsync()
    {
        broker.ConnectAsync();
    }
}
```
***

> [!Tip]
> See the <xref:Silverback.Messaging.Configuration.BrokerConnectionOptions> documentation for details about the different options.

> [!Note]
> Use <xref:Silverback.Messaging.Broker.IBrokerCollection> instead of <xref:Silverback.Messaging.Broker.IBroker> when multiple broker implementations are used.

> [!Important]
If your application is not running using an `IHost` (`GenericHost` or `WebHost`, like in a normal ASP.NET Core application) you always need to manually connect it as shown in the second example above.

## Graceful shutdown

It is important to properly close the consumers using the `DisconnectAsync` method before exiting. The offsets have to be committed and the broker has to be notified (it will then proceed to reassign the partitions as needed). Starting from version 3.0.0 this is done automatically (if your application is running using an `IHost` (`GenericHost` or `WebHost`, like in a normal ASP.NET Core application).

## Health Monitoring

The [Silverback.Integration.HealthChecks](https://www.nuget.org/packages/Silverback.Integration.HealthChecks) package contains some extensions for [Microsoft.Extensions.Diagnostics.HealthChecks](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks) that can be used to monitor the connection to the message broker.

Currently two checks exists:
* [AddOutboundEndpointsCheck](xref:Microsoft.Extensions.DependencyInjection.HealthCheckBuilderExtensions#Microsoft_Extensions_DependencyInjection_HealthCheckBuilderExtensions_AddOutboundEndpointsCheck_IHealthChecksBuilder_System_String_System_Nullable_HealthStatus__System_Nullable_IEnumerable_System_String___): Adds an health check that sends a ping message to all the outbound endpoints.
* [AddOutboxCheck](xref:Microsoft.Extensions.DependencyInjection.HealthCheckBuilderExtensions#Microsoft_Extensions_DependencyInjection_HealthCheckBuilderExtensions_AddOutboxCheck_IHealthChecksBuilder_System_String_System_Nullable_HealthStatus__System_Nullable_IEnumerable_System_String___): Adds an health check that monitors the outbound queue (outbox table), verifying that the messages are being processed.
* [AddConsumersCheck](xref:Microsoft.Extensions.DependencyInjection.HealthCheckBuilderExtensions#Microsoft_Extensions_DependencyInjection_HealthCheckBuilderExtensions_AddConsumersCheck_IHealthChecksBuilder_System_String_System_Nullable_HealthStatus__System_Nullable_IEnumerable_System_String___): Adds an health check that verifies that all consumers are connected.

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

The consumer exposes some information and statistics that can be used to programmatically check the consumer status (see <xref:Silverback.Messaging.Broker.IConsumer#Silverback_Messaging_Broker_IConsumer_StatusInfo>). A consumer can also be connected, started, stopped and disconnected at will.

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

## Samples

* [All](xref:samples)
