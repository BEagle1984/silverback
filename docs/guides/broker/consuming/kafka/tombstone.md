---
uid: consuming-tombstone
---

# Consuming Tombstone Messages

A tombstone is a Kafka message with a `null` payload. In compacted topics it is typically used to delete a record identified by its key.

In Silverback you can handle tombstones either via the inbound envelope (`IsTombstone`) or by subscribing to `Tombstone<TMessage>`.

## Subscribing to `IInboundEnvelope<TMessage>`

Use the inbound envelope when you want to handle "upsert" and "delete" in a single handler.

```csharp
public class MySubscriber
{
    public async Task HandleAsync(IInboundEnvelope<MyMessage> envelope)
    {
        if (envelope.IsTombstone)
        {
            string? key = envelope.GetKafkaKey();
            await _repository.DeleteAsync(key);
            return;
        }

        MyEntity entity = MapToEntity(envelope.Message);
        await _repository.InsertOrUpdateAsync(entity);
    }
}
```

## Subscribing to `Tombstone<TMessage>`

Subscribe separately to regular messages and tombstones.

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

- [API Reference](xref:Silverback)
- <xref:producing-tombstone>
