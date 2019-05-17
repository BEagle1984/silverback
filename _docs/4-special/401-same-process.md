---
title: Producer and consumer in the same process
permalink: /docs/special/same-process
toc: false
---

Silverback is meant to be used to integrate different microservices but you still may want to leverage the message broker to transport messages that are consumed by the same service and therefore potentially the same process.

Subscribing in the regular way would result in a sort of short circuit because both the outbound and inbound messages are being published to the same internal bus, causing them to be forwarded to the subscribers twice (before producing and after having consumed the message).

As workaround for this situation you can subscribe to an `IInboundMessage<TMessage>` instead. 

```c#
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    [Subscribe]
    public async Task OnMessageReceived(IInboundMessage<SampleMessage> message)
    {
        // ...your message handling loging...
    }
}
```

Silverback always wraps the inbound messages (received from the message broker) into an `IInboundMessage<TMessage>` and publishes both the wrapped and unwrapped message to the bus, allowing you to subscribe either of them.

**Note:** The `IInboundMessage<TMessage>` also contains a reference to the source `IEndpoint`.
{: .notice--info}

**Important!** To avoid an infinite loop you also have to disable the message unwrapping in the inbound connector.

```c#
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    ...
                },
                settings: new InboundConnectorSettings
                {
                    UnwrapMessages = false
                }));
}
```

