---
uid: headers
---

# Message Headers

## Custom headers

There are multiple ways to add custom headers to an outbound message:
* adding an enricher to the <xref:Silverback.Messaging.IProducerEndpoint>
* annotating some properties with the <xref:Silverback.Messaging.Messages.HeaderAttribute>, as shown in the next chapter.
* using a custom <xref:Silverback.Messaging.Publishing.IBehavior> or <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> can be implemented, as shown in the <xref:behaviors> and <xref:broker-behaviors> sections.

> [!Warning]
> Some message broker implementations might not support headers and Silverback doesn't currently provide any workaround, thus the headers will simply be ignored.

### Using enrichers

# [Fluent](#tab/enrichers-fluent)
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
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .AddHeader(
                        "x-my-header",
                        "static value")
                    .AddHeader<InventoryEvent>(
                        "x-product-id", 
                        envelope => envelope.Message?.ProductId)));
}
```
# [Legacy](#tab/enrichers-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    MessageEnrichers = new List<IOutboundMessageEnricher>
                    {
                        new GenericOutboundHeadersEnricher(
                            "x-my-header",
                            "static value"),
                        new GenericOutboundHeadersEnricher<InventoryEvent>(
                            "x-product-id", 
                            envelope => envelope.Message?.ProductId)
                    }
                });
}
```
***

### Using HeaderAttribute

The <xref:Silverback.Messaging.Messages.HeaderAttribute> usage is very simple: you just have to decorate the properties you want to publish as headers and specify a name for the header.

The headers value will also automatically be mapped back to the property upon consuming if the property declares a setter.

```csharp
using Silverback.Messaging.Messages;

namespace Sample
{
    public class OrderCreatedEvent
    {
        public List<LineItems> Items { get; set; }

        [Header("x-order-type", PublishDefaultValue = true)]
        [JsonIgnore]
        public OrderType OrderType { get; set; }

        [Header("x-books-order")]
        public bool ContainsBooks => Items.Any(item => item.Type == "book")

        [Header("x-dvd-order")]
        public bool ContainsDvd => Items.Any(item => item.Type == "dvd")
    }
}
```

> [!Note]
> The `PublishDefaultValue` boolean property defines whether the header has to be published even if the property is set to the default value for its data type. The default is `false`.
>
> Note that the [JsonIgnoreAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonignoreattribute) can be used to prevent the same properties to be serialized in the JSON body, when using the <xref:Silverback.Messaging.Serialization.JsonMessageSerializer>.

> [!Important]
> Only the message type will be scanned, therefore the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute> must be in the root of the message object.

## Default headers

Silverback will add some headers to the produced messages. They may vary depending on the scenario.
Here is the list of the default headers that may be sent.

Header Key | Description
:-- | :--
`x-message-id` | The [message identifier](xref:message-id).
`x-message-type` | The assembly qualified name of the message type. Used by the default <xref:Silverback.Messaging.Serialization.JsonMessageSerializer>.
`x-failed-attempts` | If an exception if thrown the failed attempts will be incremented and stored as header. This is necessary for the [error policies](xref:inbound#error-handling) to work.
`x-source-endpoint` | This will be set by the `Move` error policy and will contain the name of the endpoint the failed message is being moved from.
`x-chunk-index` | The message chunk index, used when [chunking](xref:chunking) is enabled.
`x-chunk-count` | The total number of chunks the message was split into, used when [chunking](xref:chunking) is enabled.
`x-chunk-last` | A boolean value indicating whether the message is the last one of a chunks sequence, used when [chunking](xref:chunking) is enabled.
`x-first-chunk-offset` | The <xref:Silverback.Messaging.Broker.IBrokerMessageOffset> value of the first chunk of the same message, used when [chunking](xref:chunking) is enabled.
`traceparent` | Used for distributed tracing. It is set by the <xref:Silverback.Messaging.Broker.IProducer> using the current [Activity.Id](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity). The <xref:Silverback.Messaging.Broker.IConsumer> uses it's value to set the [Activity.ParentId](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity). Note that an [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) is automatically started by the default <xref:Silverback.Messaging.Broker.IProducer> implementation. The header is implemented according to the [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1/#traceparent-header).
`tracestate` | Used for distributed tracing. It corresponds to the [Activity.TraceStateString](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity). The header is implemented according to the [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1/#tracestate-header).
`tracebaggage` | Used for distributed tracing. It corresponds to the string representation of the [Activity.Baggage](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) dictionary. This is not part of the w3c standard.
`content-type` | The content type of the [binary file](xref:binary-files), used when producing or consuming an <xref:Silverback.Messaging.Messages.IBinaryFileMessage>.
`x-kafka-message-key` | The header that will be filled with the [key](xref:kafka-partitioning) of the message consumed from Kafka.
`x-kafka-message-timestamp` | The header that will be filled with the timestamp of the message consumed from Kafka.

The static classes <xref:Silverback.Messaging.Messages.DefaultMessageHeaders> and <xref:Silverback.Messaging.Messages.KafkaMessageHeaders> contain all default header names constants.

## Customizing header names

The default header names can be overridden using the `WithCustomHeaderName` configuration method.

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
            .WithCustomHeaderName(DefaultMessageHeaders.ChunkId, "x-ch-id")
            .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-ch-cnt"));
    }
}
