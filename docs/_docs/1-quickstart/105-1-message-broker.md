---
title: Connecting to a Message Broker
permalink: /docs/quickstart/message-broker
---

To connect Silverback to a message broker we need a reference to `Silverback.Integration`, plus the concrete implementation (`Silverback.Integration.Kafka`  or `Silverback.Integration.RabbitMQ`). We can then add the broker to the DI and configure the connected endpoints. 

## Sample configuration

The following example is very simple and there are of course many more configurations and possibilities. Some more details are given in the dedicated [Broker Configuration]({{ site.baseurl }}/docs/configuration/endpoint) section.

The basic concepts:
* `WithConnectionToMessageBroker` registers the services necessary to connect to a message broker
* `AddKafka`, `AddRabbit`, `AddInMemoryBroker`, etc. register the message broker implementation(s)
* `AddInbound` is used to automatically relay the incoming messages to the internal bus and they can therefore be subscribed as seen in the previous chapters
* `AddOutbound` works the other way around and subscribes to the internal bus to forward the integration messages to the message broker
* `Connect` automatically creates and starts all the consumers.

### Basic Kafka configuration

The following sample demonstrates how to setup some inbound and outbound endpoints against an Apache Kafka broker.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddInboundConnector()
                .AddOutboundConnector());
    }

    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator.Connect(endpoints => endpoints
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
                }));
    }
}
{% endhighlight %}
</figure>

### Basic RabbitMQ configuration

The following sample demonstrates how to setup some inbound and outbound endpoints against a RabbitMQ broker.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddRabbit()
                .AddInboundConnector()
                .AddOutboundConnector());
    }

    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator.Connect(endpoints => endpoints
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
                }));
    }
}
{% endhighlight %}
</figure>

### Multiple brokers

It is possible to use multiple message brokers together in the same application. The following sample demonstrates how to consume from both Apache Kafka and RabbitMQ.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddRabbit()
                .AddInboundConnector());
    }

    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator.Connect(endpoints => endpoints
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("rabbit-events")
                {
                    ...
                })
            .AddInbound(
                new KafkaConsumerEndpoint("kafka-events")
                {
                    ...
                }));
    }
}
{% endhighlight %}
</figure>

## Using IEndpointsConfigurator

The endpoints configuration can be split into multiple types implementing the `IEndpointsConfigurator` interface.

```csharp
private class MyFeatureConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddOutbound<IMyFeatureEvents>(
                new KafkaProducerEndpoint("my-feature-events")
                {
                    ...
                }
            )
            .AddInbound(
                new KafkaConsumerEndpoint("my-feature-commands")
                {
                    ...
                }
            );
    }
}
```

The configurators can be registered using either the `RegisterConfigurator` or `AddEndpointsConfigurator` methods.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .RegisterConfigurator<MyFeatureConfigurator>());
    }
}
{% endhighlight %}
</figure>
...or...
<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka());

        services.AddEndpointsConfigurator<MyFeatureConfigurator>());
    }
}
{% endhighlight %}
</figure>

### Using assembly scanning

You can of course use the assembly scanning capabilities of your  Dependency Injection framework (e.g. [Autofac](https://autofac.org/)) to register all the `IEndpointsConfigurator`.

Example using Autofac:
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

## Graceful shutdown

It is important to properly close the consumers using the `Disconnect` method before exiting. The offsets have to be committed and the broker has to be notified (it will then proceed to reassign the partitions as needed).

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(IBusConfigurator busConfigurator)
    {
        var brokers = busConfigurator.Connect(...);

        appLifetime.ApplicationStopping.Register(() => brokers.Disconnect());
    }
}
{% endhighlight %}
</figure>

## Health Monitoring

The `Silverback.Integration.HealthChecks` package contains some extensions for `Microsoft.Extensions.Diagnostics.HealthChecks` that can be used to monitor the connection to the message broker.

Currently two checks exists:
* `AddOutboundEndpointsCheck`: Adds an health check that sends a ping message to all the outbound endpoints.
* `AddOutboundQueueCheck`: Adds an health check that monitors the outbound queue (outbox table), verifying that the messages are being processed.

The usage is very simple, you just need to configure the checks in the Startup.cs, as shown in the following example.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddOutboundEndpointsCheck()
            .AddOutboundQueueCheck();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");
    }
}
{% endhighlight %}
</figure>



