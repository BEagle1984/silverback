---
title: Kafka Message Key (partitioning)
permalink: /docs/kafka/message-key
toc: false
---

Apache Kafka require a message key for different purposes, such as:
* **Partitioning**: Kafka can guarantee ordering only inside the same partition and it is therefore important to be able to route correlated messages into the same partition. To do so you need to specify a key for each message and Kafka will put all messages with the same key in the same partition.
* **Compacting topics**: A topic can be configured with `cleanup.policy=compact` to instruct Kafka to keep only the latest message related to a certain object, identified by the message key. In other words Kafka will retain only 1 message per each key value.

Silverback offers a convenient way to specify the message key. It is enough to decorate the properties that must be part of the key with `KafkaKeyMemberAttribute`.

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

If no key members are specified no key will be generated and the messages will land in a random partition.

The message key will also be submitted as header (see [Default Message Headers]({{ site.baseurl }}/docs/advanced/headers) for details).
{: .notice--note}
