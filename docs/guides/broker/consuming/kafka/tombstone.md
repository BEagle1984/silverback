---
uid: consuming-tombstone
---

# Consuming Tombstone Messages

A tombstone message is a message with an empty payload. It is usually used in Kafka to signal that a message with a specific key should be deleted, and the message key is used to identify the specific record to be deleted.

Consuming them is straightforward and doesn't require any special configuration. The subscriber must be designed to subscribe and handle them, though.

There are mainly two ways to subscribe to tombstone messages:
* Subscribing to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and checking the `IsTombstone` property
* Subscribing to a <xref:Silverback.Messaging.Messages.Tombstone`1>

## Subscribing to IInboundEnvelope

Subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> to get the message plus all metadata (headers, broker-specific data, etc.). The envelope exposes a convenient `IsTombstone` boolean property which can be checked and the extension method `GetKafkaKey()` can be used to get the key of the message.

```csharp
public class MySubscriber
{
    public async Task HandleAsync(IInboundEnvelope<MyMessage> envelope)
    {
        if (envelope.IsTombstone)
        {
            string key = envelope.GetKafkaKey();
            await _repository.DeleteAsync(key);
        }
        else
        {
            MyEntity entity = MapToEntity(envelope.Message);
            await _repository.InsertOrUpdateAsync(entity);
        }
    }
}
```

## Subscribing to Tombstone

In this case you actually need to separately subscribe to the actual message or the <xref:Silverback.Messaging.Messages.Tombstone`1>.

```csharp
public class MySubscriber
{
    public async Task HandleAsync(MyMessage message)
    {
        MyEntity entity = MapToEntity(message);
        await _repository.InsertOrUpdateAsync(entity);
    }

    public async Task HandleAsync(Tombstone<MyMessage> tombstone)
    {
        string key = tombstone.MessageKey;
        await _repository.DeleteAsync(key);
    }
}
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:producing-tombstone> guide
