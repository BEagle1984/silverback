# Routing Key

With RabbitMQ a routing key can be used to route the messages to a specific queue or filter the messages in a topic. See also the [routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet.html) and [topics](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html) tutorials on the official RabbitMQ web site. 

<figure>
	<a href="~/images/diagrams/rabbit-routing.png"><img src="~/images/diagrams/rabbit-routing.png"></a>
    <figcaption>The messages are routed according to the routing key.</figcaption>
</figure>

Silverback offers a convenient way to specify the routing key, using the <xref:Silverback.Messaging.Messages.RabbitRoutingKeyAttribute>.

```csharp
public class MyMessage : IIntegrationMessage
{
    [RabbitRoutingKey]
    public string Key { get; set; }
    
    ...
}
```
