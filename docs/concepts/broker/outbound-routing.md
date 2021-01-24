---
uid: outbound-routing
---

# Outbound Messages Routing

By default Silverback routes the messages according to their type and the static configuration defined at startup. In some cases you may need more flexibility, being able to apply your own routing rules.

In such cases it is possible to either take advantage of the simple endpoint name resolvers or even implement a fully customized router.

<figure>
	<a href="~/images/diagrams/outbound-routing.png"><img src="~/images/diagrams/outbound-routing.png"></a>
    <figcaption>The messages are dynamically routed to the appropriate endpoint.</figcaption>
</figure>

## Endpoint name resolver

Using an endpoint name resolver is fairly simple and just requires a slightly different configuration in the <xref:Silverback.Messaging.IProducerEndpoint>.

Here below a few examples of custom routing. Please refer to the <xref:Silverback.Messaging.KafkaProducerEndpoint>/<xref:Silverback.Messaging.Configuration.Kafka.IKafkaProducerEndpointBuilder> or <xref:Silverback.Messaging.MqttProducerEndpoint>/<xref:Silverback.Messaging.Configuration.Mqtt.IMqttProducerEndpointBuilder> API documentation for further information about all the possibilities. 

# [Fluent](#tab/resolvers-fluent)
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
                
                // Using a resolver function
                .AddOutbound<OrderCreatedEvent>(endpoint => endpoint
                    .ProduceTo<OrderCreatedEvent>(envelope => 
                    {
                        if (envelope.Message.IsPriority)
                            return "priority-orders";
                        else
                            return "normal-orders";
                    }))
                
                // Using format string and arguments function
                .AddOutbound<OrderCreatedEvent>(endpoint => endpoint
                    .ProduceTo<OrderCreatedEvent>(
                        "orders-{0}",
                        envelope => 
                        {
                            if (envelope.Message.IsPriority)
                                return new[] { "priority" };
                            else
                                return new[] { "normal" };
                        }))
                
                // Using a resolver class
                .AddOutbound<OrderCreatedEvent>(endpoint => endpoint
                    .UseEndpointNameResolver<MyEndpointNameResolver>())
                
                // Kafka only: using a partition resolver function
                .AddOutbound<InventoryUpdateMessage>(endpoint => endpoint
                    .ProduceTo<InventoryUpdateMessage>(
                        _ => "topic1",
                        envelope =>
                        {
                            switch (envelope.Message.Supplier)
                            {
                                case "foo":
                                    return 0;
                                case "bar":
                                    return 1;
                                case "baz":
                                    return 2;
                            }
                        }))));
}
```
# [Legacy](#tab/resolvers-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            
            // Using a resolver function
            .AddOutbound<OrderCreatedEvent>(
                new KafkaProducerEndpoint(envelope => 
                    {
                        var message = (OrderCreatedEvent) envelope.Message;
                        
                        if (message.IsPriority)
                            return "priority-orders";
                        else
                            return "normal-orders";
                    })
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    }
                })
            
            // Using format string and arguments function
            .AddOutbound<OrderCreatedEvent>(
                new KafkaProducerEndpoint(
                    "orders-{0}",
                    envelope => 
                    {
                        var message = (OrderCreatedEvent) envelope.Message;
                        
                        if (message.IsPriority)
                            return new[] { "priority" };
                        else
                            return new[] { "normal" };
                    })
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    }
                })
            
            // Using a resolver class
            .AddOutbound<OrderCreatedEvent>(
                new KafkaProducerEndpoint(typeof(MyEndpointNameResolver))
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    }
                })
            
            // Kafka only: using a partition resolver function
            .AddOutbound<InventoryUpdateMessage>(
                new KafkaProducerEndpoint(
                    _ => "topic1",
                    envelope =>
                    {
                        var message = (InventoryUpdateMessage) envelope.Message;
                        
                        switch (message.Supplier)
                        {
                            case "foo":
                                return 0;
                            case "bar":
                                return 1;
                            case "baz":
                                return 2;
                        }
                    })
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    }
                });
}
```
# [ProducerEndpointNameResolver](#tab/resolvers-resolver)
```csharp
public class MyEndpointNameResolver : ProducerEndpointNameResolver<TestEventOne>
{
    private readonly IMyService _service;
    
    public MyEndpointNameResolver(IMyService service)
    {
        _service = service;
    }

    protected override string GetName(IOutboundEnvelope<TestEventOne> envelope)
    {
        if (_service.IsPriorityOrder(envelope.Message.OrderNumber))
            return "priority-orders";
        else
            return "normal-orders";
    }
}
```
***

## Custom router

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
***
