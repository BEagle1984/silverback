---
title: Kafka Partitioning
permalink: /docs/configuration/partitioning
---

Kafka can guarantee ordering only inside the same partition and it is therefore important to be able to route correlated messages into the same partition.

To do so you need to specify a key for each message and Kafka will put all messages with the same key in the same partition.

Silverback offers a convenient way to specify the message key. It is enough to decorate the properties that must be part of the key with `KeyMemberAttribute`.

```c#
public class MultipleKeyMembersMessage : IIntegrationMessage
{
    public Guid Id { get; set; }

    [KeyMember]
    public string One { get; set; }
    
    [KeyMember]
    public string Two { get; set; }

    public string Three { get; set; }
}
```

If no key members are specified no key will be generated and the messages will land in a random partition.