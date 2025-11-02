---
uid: consuming-headers
---

# Reading Headers

Both Kafka and MQTT 5 support custom headers (called user properties in MQTT). With Silverback there are basic two ways to access headers, see below.

## Subscribing to IInboundEnvelope

Subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> to get the message plus all metadata (headers, broker-specific data, etc.). The envelope exposes a `Headers` collection that contains the headers published with the message.

```csharp
public class MySubscriber
{
    public Task HandleAsync(IInboundEnvelope<MyMessage> envelope)
    {
        int priority = envelope.Headers.GetValue<int>("x-priority");
        
        ...
    }
}
```

## HeaderAttribute on the Message Model

Using the <xref:Silverback.Messaging.Messages.HeaderAttribute> is straightforward: simply decorate the properties you want to be mapped to a header, and Silverback will take care of filling the model with the values from the respective headers.

```csharp
using Silverback.Messaging.Messages;

public class OrderCreatedEvent
{
    [Header("x-order-type")]
    [JsonIgnore]
    public OrderType OrderType { get; set; }

    [Header("x-books-order")]
    public bool ContainsBooks => Items.Any(item => item.Type == "book");
}
```

> [!Important]
> Only the message type will be scanned, therefore the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute> must be in the root of the message object.

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:consuming-headers> guide
* <xref:default-headers>
