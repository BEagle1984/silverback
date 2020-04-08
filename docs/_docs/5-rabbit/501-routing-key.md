---
title: Routing Key
permalink: /docs/rabbit/routing-key
toc: false
---

With RabbitMQ a routing key can be used to route the mssages to a specific queue or filter the messages in a topic.

Silverback offers a convenient way to specify the routing key. It is enough to decorate a property with `RabbitRoutingKeyAttribute`.

```csharp
public class MyMessage : IIntegrationMessage
{
    public Guid Id { get; set; }

    [RabbitRoutingKey]
    public string Key { get; set; }
    
    ...
}
```
