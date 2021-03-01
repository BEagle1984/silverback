---
uid: tombstone
---

# Tombstone Message

A tombstone message is a message with a `null` body, used to indicate that the record has been deleted. This technique is for example used with Kafka topics compaction, to get rid of obsolete records.

## Consumer

Silverback maps by default the messages with a `null` body to a <xref:Silverback.Messaging.Messages.Tombstone> or <xref:Silverback.Messaging.Messages.Tombstone`1>. This behavior can be changed setting using the [SkipNullMessages](xref:Silverback.Messaging.Configuration.IConsumerEndpointBuilder`1#Silverback_Messaging_Configuration_IConsumerEndpointBuilder_1_SkipNullMessages) or [UseLegacyNullMessageHandling](xref:Silverback.Messaging.Configuration.IConsumerEndpointBuilder`1#Silverback_Messaging_Configuration_IConsumerEndpointBuilder_1_UseLegacyNullMessageHandling) of the <xref:Silverback.Messaging.Configuration.IConsumerEndpointBuilder`1>, or setting the [NullMessageHandlingStrategy](xref:Silverback.Messaging.ConsumerEndpoint#Silverback_Messaging_ConsumerEndpoint_NullMessageHandlingStrategy) property of the <xref:Silverback.Messaging.ConsumerEndpoint>).

The <xref:Silverback.Messaging.Messages.Tombstone>/<xref:Silverback.Messaging.Messages.Tombstone`1> message exposes a single property containing the [message identifier](xref:message-id). 

# [EndpointConfigurator](#tab/consumer-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("catalog-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DeserializeJson(serializer => serializer
                        .UseFixedType<Product>())));
}
```
# [Subscriber](#tab/consumer-subscriber)
```csharp
public class MySubscriber
{
    public async Task OnProductDeleted(
      Tombstone<Product> tombstone)
    {
        // TODO: use tombstone.MessageId 
        // to remove the product from the
        // local database
    }
}
```
***

> [!Important]
> In order to create a typed <xref:Silverback.Messaging.Messages.Tombstone`1> it is required that either the consumed message declares the `x-message-type` [header](xref:headers) or a fixed type deserializer is used (as shown in the example above). Otherwise the `null` message will be mapped to a simple <xref:Silverback.Messaging.Messages.Tombstone>.

## Producer

A <xref:Silverback.Messaging.Messages.Tombstone`1> (or <xref:Silverback.Messaging.Messages.Tombstone>) can also be used to produce a `null` message.

# [EndpointConfigurator](#tab/producer-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<Product>(endpoint => endpoint
                    .ProduceTo("catalog-events")));
}
```
# [Publisher](#tab/producer-publisher)
```csharp
public class MyService
{
    private readonly IPublisher _publisher;
    
    public MyService(IPublisher publisher)
    {
        _publisher = publisher;
    }
    
    public async Task DeleteProduct(string productId)
    {
        ...
        
        await _publisher.PublishAsync(new Tombstone<Product>(productId);
    }
```
***

> [!Note]
> The <xref:Silverback.Messaging.Messages.Tombstone`1> messages are routed according to the type parameter `TMessage`. This means that they will be published to the outbound endpoints papped to the same `TMessage` (`Product` in the above example), as well as to the outbound endpoints explicitly mapping <xref:Silverback.Messaging.Messages.Tombstone>.

