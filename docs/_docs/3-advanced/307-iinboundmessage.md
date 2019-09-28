---
title: IInboundMessage
permalink: /docs/advanced/iinboundmessage

toc: false
---

When a message is consumed Silverback will wrap it into an `IInboundMessage` and push it through the message bus. It will afterwords be unboxed and pushed again into the bus in its original form, so that you can subscribe to the pure message model.

You can anyhow take advantage of this internal mechanic to gain access to the transport information of the message, since the `IInboundMessage` holds all endpoint, offset and headers data.

Subscribing to the `IInboundMessage` works exactly the same as subscribing to any other message.

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