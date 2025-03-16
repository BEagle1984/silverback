---
uid: producing-headers
---

# Setting Headers

Both Kafka and MQTT 5 support custom headers (called user properties in MQTT). With Silverback there are many ways to set headers.

## Enrichers in the Endpoint Configuration

Using an enricher configured fro the endpoint you can add static headers to all produced messages.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .AddHeader("header1", "value1")
                .AddHeader("header2", "value2"))));
```

Or you can add headers whose values are computed from the message (or the envelope).

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .AddHeader("header1", message => message.Property1)
                .AddHeader("header2", envelope => envelope.Message.Property2))));
```

## HeaderAttribute in the Message Model

Using the <xref:Silverback.Messaging.Messages.HeaderAttribute> is straightforward: simply decorate the properties you want to publish as headers and specify an header name.

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

> [!Note]
> The [JsonIgnoreAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonignoreattribute) can be used to prevent the same properties to be serialized in the JSON body, when using the <xref:Silverback.Messaging.Serialization.JsonMessageSerializer>.

> [!Important]
> Only the message type will be scanned, therefore the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute> must be in the root of the message object.

## Using WrapAndPublish / WrapAndPublishBatch

The `WrapAndPublish`, `WrapAndPublishAsync`, `WrapAndPublishBatch` and `WrapAndPublishBatchAsync` methods of the `IPublisher` can be used to wrap the messages in an envelope and add additional metadata, such as headers.

```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { ... },
    envelope => envelope
        .AddHeader("x-source", "my-app")
        .AddHeader("x-priority", envelope.Message.Priority));
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:consuming-headers> guide
* <xref:default-headers>
