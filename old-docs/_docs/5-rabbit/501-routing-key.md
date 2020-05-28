---
title: Routing Key
permalink: /docs/rabbit/routing-key
toc: false
---

With RabbitMQ a routing key can be used to route the mssages to a specific queue or filter the messages in a topic. See also the [routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet.html) and [topics](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html) tutorials on the official RabbitMQ web site. 

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/rabbit-routing.png"><img src="{{ site.baseurl }}/assets/images/diagrams/rabbit-routing.png"></a>
    <figcaption>The messages are routed according to the routing key.</figcaption>
</figure>

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
