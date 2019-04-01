---
title: Custom Serializer
permalink: /docs/advanced/custom-serializer
---

The default `JsonMessageSerializer` internally uses `Newtonsoft.Json` and leverages its [automatic type handling](https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm). If few words `Newtonsoft.Json` adds a _$type_ property to the JSON, containing the serialized type fully qualified name and uses that information to resolve the type when deserializing.

This is the suggested serialization strategy when both producer and consumer are based on _Silverback_ but may not be ideal for interoperability.

## Deserializing a JSON interoperable message

Chances are that the type name is not being delivered as part of the message, if you are consuming a message coming from another system (not based on _Silverback_).

In that case you can resort to `JsonMessageSerializer<TMessage>`. This serializer is also based on `Newtonsoft.Json` but the message type is hard-coded.

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

In some cases you may want to build your own custom serializer extending `IMessageSerializer` directly.

```c#
public class MyCustomSerializer : IMessageSerializer
{
    public byte[] Serialize(object message)
    {
        ...
    }

    public object Deserialize(byte[] message)
    {
        ...
    }
}
```