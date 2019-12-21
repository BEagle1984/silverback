---
title: Serialization
permalink: /docs/advanced/serialization
---

Being flexible when serializing and deserializing the messages sent over the message broker is crucial for interoperability and these mechanisms are therfore completely customizable.

## Default JsonMessageSerializer

The default `JsonMessageSerializer` internally uses `Newtonsoft.Json` to serialize the messages as json. The messages are then transformed in a byte array using the UTF8 encoder.

A few headers are added to the message, in particular `x-message-type` is used by the `JsonMessageSerializer` to know the message type when deserializing. It also leverages the Newtonsoft's [automatic type handling](https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm) to automatically resolve the actual type of the nested properties.

The deserializer function provided by `JsonMessageSerializer` will obviously try to map the message to a type with the exact assembly qualified name found in the `x-message-type` header. It is therefore a good practice to share the message models among the services, maybe through nuget.

This is the suggested serialization strategy when both producer and consumer are based on Silverback but may not be ideal for interoperability.

Have a look at the [Default Message Headers]({{ site.baseurl }}/docs/advanced/headers) section for an overview on the headers that are appended to the messages.

## Typed JsonMessageSerializer

If you are consuming a message coming from another system (not based on Silverback), chances are that the type name is not being delivered as header.

In that case you can resort to the typed `JsonMessageSerializer<TMessage>`. This serializer works like the default one but the message type is hard-coded, instead of being expected in the header.

```c#
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    ...
                    Serializer = new JsonMessageSerializer<OrderEvent>
                }));
}
```

**Note:** The `JsonMessageSerializer` can be also be tweaked modifying its `Settings` and `Encoding`.
{: .notice--info}

## Custom IMessageSerializer

In some cases you may want to build your very own custom serializer extending `IMessageSerializer` directly.

```c#
public class MyCustomSerializer : IMessageSerializer
{
    public byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
    {
        ...
    }

    public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
    {
        ...
    }
}
```
