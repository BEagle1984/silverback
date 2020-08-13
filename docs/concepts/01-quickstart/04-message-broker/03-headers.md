---
uid: headers
---

# Message Headers

## Custom headers

There are multiple options to add custom headers to a message:
* using an `IBehavior` or `IProducerBehavior`, as shown in the <xref:behaviors> chapter of the quickstart
* annotating some properties with the `HeaderAttribute` (see next chapter)
* or you could use the `IBroker` / `IProducer` directly, as explained in the <xref:ibroker> section

### Using HeaderAttribute

The `HeaderAttribute` usage is very simple: you just have to decorate the properties you want to publish as headers and specify a name for the header.

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
> Note that the `JsonIgnoreAttribute` can be used to prevent the same properties to be serialized in the JSON body, when using the `JsonMessageSerializer`.

> [!Important]
> Only the message type will be scanned, therefore the properties decorated with the `HeaderAttribute` must be in the root of the message object.

## Default headers

Silverback will add some headers to the produced messages. They may vary depending on the scenario.
Here is the list of the default headers that may be sent.

Header Key | Description
:-- | :--
`x-message-id` | The [message identifier](xref:xref:message-id).
`x-message-type` | The assembly qualified name of the message type. Used by the default `JsonMessageSerializer`.
`x-failed-attempts` | If an exception if thrown the failed attempts will be incremented and stored as header. This is necessary for the [error policies](xref:inbound#error-handling) to work.
`x-source-endpoint` | This will be set by the `Move` error policy and will contain the name of the endpoint the failed message is being moved from.
`x-chunk-id` | The unique id of the message chunk, used when [chunking](xref:chunking) is enabled.
`x-chunks-count` | The total number of chunks the message was split into, used when [chunking](xref:chunking) is enabled.
`x-first-chunk-offset` | The `IOffset` value of the first chunk of the same message, used when [chunking](xref:chunking) is enabled.
`x-batch-id` | The unique id assigned to the messages batch, used mostly for tracing, when [batch processing](xref:inbound#batch-processing) is enabled.
`x-batch-size` | The total number of messages in the batch, used mostly for tracing, when [batch processing](xref:inbound#batch-processing) is enabled.
`traceparent` | The current `Activity.Id`, used by the `IConsumer` implementation to set the `Activity.ParentId`, thus enabling distributed tracing across the message broker. Note that an `Activity` is automatically started by the default `IProducer` implementation. See [System.Diagnostics documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1) for details about `Activity` and distributed tracing in asp.net core and [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1) for details about the headers.
`tracestate` | The `Activity.TraceStateString`. See also the [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1) for details.
`tracebaggage` | The string representation of the `Activity.Baggage` dictionary. See [System.Diagnostics documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1) for details.
`content-type` | The content type of the [binary file](xref:binary-files), used when producing or consuming an `IBinaryFileMessage`.
`x-kafka-message-key` | When using Kafka, the [kafka message key](xref:kafka-message-key) will also be submitted as header (see <xref:kafka-message-key> to know how to define a message key)

Some constants for the headers name are also provided as reported in the following table.

Header Key | Constant
:-- | :--
`x-message-id` | `DefaultMessageHeaders.MessageId`
`x-message-type` | `DefaultMessageHeaders.MessageType`
`x-failed-attempts` | `DefaultMessageHeaders.FailedAttempts`
`x-source-endpoint` | `DefaultMessageHeaders.SourceEndpoint`
`x-chunk-id` | `DefaultMessageHeaders.ChunkId`
`x-chunks-count` | `DefaultMessageHeaders.ChunksCount`
`x-first-chunk-offset` | `DefaultMessageHeaders.FirstChunkOffset`
`x-batch-id` | `DefaultMessageHeaders.BatchId`
`x-batch-size` | `DefaultMessageHeaders.BatchSize`
`traceparent` | `DefaultMessageHeaders.TraceId`
`tracestate` | `DefaultMessageHeaders.TraceState`
`tracebaggage` | `DefaultMessageHeaders.TraceBaggage`
`content-type` | `DefaultMessageHeaders.ContentType`
`x-kafka-message-key` | `KafkaMessageHeaders.KafkaKey`

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
