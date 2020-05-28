---
uid: kafka-message-key
---

# Kafka Message Key (partitioning)

Apache Kafka require a message key for different purposes, such as:
* **Partitioning**: Kafka can guarantee ordering only inside the same partition and it is therefore important to be able to route correlated messages into the same partition. To do so you need to specify a key for each message and Kafka will put all messages with the same key in the same partition.
* **Compacting topics**: A topic can be configured with `cleanup.policy=compact` to instruct Kafka to keep only the latest message related to a certain object, identified by the message key. In other words Kafka will retain only 1 message per each key value.

<figure>
	<a href="~/images/diagrams/kafka-key.png"><img src="~/images/diagrams/kafka-key.png"></a>
    <figcaption>The messages with the same key are guaranteed to be written to the same partition.</figcaption>
</figure>

Silverback will always generate a message key (same value as the `x-message-id` [header](xref:headers)) but it also offers a convenient way to specify a custom key. It is enough to decorate the properties that must be part of the key with `KafkaKeyMemberAttribute`.

```csharp
public class MultipleKeyMembersMessage : IIntegrationMessage
{
    public Guid Id { get; set; }

    [KafkaKeyMember]
    public string One { get; set; }
    
    [KafkaKeyMember]
    public string Two { get; set; }

    public string Three { get; set; }
}
```

> [!Note]
> The message key will also be received as header (see <xref:headers> for details).
