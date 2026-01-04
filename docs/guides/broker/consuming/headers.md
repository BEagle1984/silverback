---
uid: consuming-headers
---

# Reading Headers

Kafka and MQTT 5 support custom headers (called *user properties* in MQTT). In Silverback you can access them either via the inbound envelope or by mapping them to model properties.

## Subscribe to `IInboundEnvelope<TMessage>`

Subscribe to <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> to access the message and its metadata (headers, broker-specific data, etc.). The envelope exposes a `Headers` collection.

```csharp
public class MySubscriber
{
    public async Task HandleAsync(IInboundEnvelope<MyMessage> envelope)
    {
        int priority = envelope.Headers.GetValue<int>("x-priority");

        // ...
    }
}
```

## Use `HeaderAttribute` on the message model

Decorate message properties with <xref:Silverback.Messaging.Messages.HeaderAttribute> to map header values into the model.

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
> Only the message type is scanned. Properties decorated with <xref:Silverback.Messaging.Messages.HeaderAttribute> must be declared on the root message object (not nested types).

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:default-headers>
- <xref:producing-headers>
