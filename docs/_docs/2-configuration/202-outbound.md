---
title: Outbound Connector
permalink: /docs/configuration/outbound
---

The outbound connector is used to automatically relay the integration messages (published to the internal bus) to the message broker. Multiple outbound endpoints can be configured and Silverback will route the messages according to their type (based on the `TMessage` parameter passed to the `AddOutbound<TMessage>` method.

## Implementations

Multiple implementations of the connector are available, offering a variable degree of reliability.

### Basic

The basic `OutboundConnector` is very simple and relays the messages synchronously. This is the easiest, better performing and most lightweight option but it doesn't allow for any transactionality (once the message is fired, is fired) nor resiliency to the message broker failure.

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/outbound-basic.png"><img src="{{ site.baseurl }}/assets/images/diagrams/outbound-basic.png"></a>
    <figcaption>Messages 1, 2 and 3 are directly produced to the message broker.</figcaption>
</figure>

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
                .AddOutboundConnector());
    }

    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("basket-events")
                    {
                        ...
                    }));
    }
}
{% endhighlight %}
</figure>

### Deferred

The `DeferredOutboundConnector` stores the messages into a local queue to be forwarded to the message broker in a separate step.

This approach has two main advantages:
1. Fault tollerance: you depend on the database only and if the message broker is unavailable the produce will be automatically retried later on
1. Transactionality: when using the database a storage you can commit the changes to the local database and the outbound messages inside a single atomic transaction (this pattern is called [transactional outbox](https://microservices.io/patterns/data/transactional-outbox.html))

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/outbound-outboxtable.png"><img src="{{ site.baseurl }}/assets/images/diagrams/outbound-outboxtable.png"></a>
    <figcaption>Messages 1, 2 and 3 are stored in the outbox table and produced by a separate thread or process.</figcaption>
</figure>

#### Database outbox table

The `DbOutboundConnector` will store the outbound messages into a database table.

When using entity framework (`UseDbContext<TDbContext>` contained in the `Silverback.EntityFrameworkCore` package) the outbound messages are stored into a `DbSet` and are therefore implicitly saved in the same transaction used to save all other changes.

The `DbContext` must include a `DbSet<OutboundMessage>` and an `OutboundWorker` is to be started to process the outbound queue. See also the [sample DbContext]({{ site.baseurl }}/docs/extra/dbcontext).
{: .notice--note}

The current `OutboundWorker` cannot be horizontally scaled and starting multiple instances will cause the messages to be produced multiple times. In the following example a distributed lock in the database is used to ensure that only one instance is running and another one will _immediatly_ take over when it stops (the `DbContext` must include a `DbSet<Lock>` as well, see also the [sample DbContext]({{ site.baseurl }}/docs/extra/dbcontext)). 
{: .notice--important}

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()

            // Initialize Silverback to use MyDbContext as database storage.
            .UseDbContext<MyDbContext>()

            // Setup the lock manager using the database
            // to handle the distributed locks.
            // If this line is omitted the OutboundWorker will still
            // work without locking. 
            .AddDbDistributedLockManager()

            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                // Use a deferred outbound connector
                .AddDbOutboundConnector()

                // Add the IHostedService processing the outbound queue
                // (overloads are available to specify custom interval,
                // lock timeout, etc.)
                .AddDbOutboundWorker();
    }
}
{% endhighlight %}
</figure>

#### Custom outbound queue

You can easily create another implementation targeting another kind of storage, simply creating your own `IOutboundQueueProducer` and `IOutboundQueueConsumer` and plug them in.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddDbDistributedLockManager()

            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddOutboundConnector<SomeCustomQueueProducer>()
                .AddOutboundWorker<SomeCustomQueueConsumer>();
    }
}
{% endhighlight %}
</figure>

## Subscribing locally

The published messages that are routed to an outbound endpoint cannot be subscribed locally (within the same process), unless explicitely desired.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(...)
                .PublishOutboundMessagesToInternalBus()
            );
    }
}
{% endhighlight %}
</figure>

What said above is only partially true, as you can subscribe to the wrapped message (`IOutboundEnvelope<TMessage>`) even without calling `PublishOutboundMessagesToInternalBus`.
{: .notice--note}

## Producing the same message to multiple endpoints

An outbound route can point to multiple endpoints resulting in every message being broadcasted to all endpoints.

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/outbound-broadcast.png"><img src="{{ site.baseurl }}/assets/images/diagrams/outbound-broadcast.png"></a>
    <figcaption>Messages 1, 2 and 3 are published to both topics simultaneously.</figcaption>
</figure>

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
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
{% endhighlight %}
</figure>

A message will also be routed to all outbound endpoint mapped to a type that matches the message type. In the example below an `OrderCreatedMessage` (that inherits from `OrderMessage`) would be sent to both endpoints.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddOutbound<OrderMessage>(
                    new KafkaProducerEndpoint("topic-1")
                    {
                        ...
                    })
                .AddOutbound<OrderCreatedMessage>(
                    new KafkaProducerEndpoint("topic-1")
                    {
                        ...
                    }));
    }
}
{% endhighlight %}
</figure>

## Dynamic custom routing

By default Silverback routes the messages according to their type and the static configuration defined at startup. In some cases you may need more flexibility, being able to apply your own routing rules. In such cases it is possible to implement a fully customized router.

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/outbound-customrouting.png"><img src="{{ site.baseurl }}/assets/images/diagrams/outbound-customrouting.png"></a>
    <figcaption>The messages are dynamically routed to the appropriate endpoint.</figcaption>
</figure>

In the following example a custom router is used to route the messages according to their priority (a copy is also sent to a catch-all topic).

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

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddSingletonOutboundRouter<PrioritizedRouter>();
    }

    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddOutbound<IPrioritizedCommand, PrioritizedRouter>());
    }
}
{% endhighlight %}
</figure>

The outbound routers can only be registered as singleton. If a scoped instance is needed you have to inject the `IServiceScopeFactory` (or `IServiceProvider`).
{: .notice--note}

