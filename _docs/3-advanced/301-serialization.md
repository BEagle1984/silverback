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

### Message Headers

Here is the list of headers that Silverback may add to the produced messages, depending on the scenario:

Header | Optional | Description
:-- | :-: | :--
`x-message-id` | (yes) | A unique identifier that may be useful for tracing. It may not be present if the produced message isn't implementing `IIntegrationMessage` and no `Id` or `MessageId` property of a supported type is defined.
`x-message-type` | no | The assembly qualified name of the message type.
`x-failed-attempts` | yes | If an exception if thrown the failed attempts will be incremented and stored as header. This is necessary for the [error policies]({{ site.baseurl }}/docs/configuration/inbound#error-handling) to work.
`x-chunk-id` | yes | The unique id of the message chunk, used when [chunking]({{ site.baseurl }}/docs/advanced/chunking) is enabled.
`x-chunks-count` | yes | The total number of chunks the message was splitted into, used when [chunking]({{ site.baseurl }}/docs/advanced/chunking) is enabled.
`x-batch-id` | yes | The unique id assigned to the messages batch, used mostly for tracing, when [batch processing]({{ site.baseurl }}/docs/configuration/inbound#batch-processing) is enabled.
`x-batch-size` | yes | The total number of messages in the batch, used mostly for tracing, when [batch processing]({{ site.baseurl }}/docs/configuration/inbound#batch-processing) is enabled.

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
