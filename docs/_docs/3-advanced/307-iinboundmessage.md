---
title: IInboundMessage
permalink: /docs/advanced/iinboundmessage

toc: false
---

When a message is consumed Silverback wraps it into an `IInboundMessage` and pushs it to the message bus. Afterwards the message will be unwrapped and pushed again to the message bus, in its pure form. That means you can subscribe to receive the wrapped message and also the pure message.

You can take advantage of this mechanism to gain access to the transport information of the message, since the `IInboundMessage` holds all this information like endpoint, offset and headers data.

Subscribing to the `IInboundMessage` works exactly the same as subscribing to any other message.

```c#
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    public async Task OnWrappedMessageReceived(IInboundMessage<SampleMessage> message)
    {
        // ...your message handling logic...
    }

    public async Task OnPureMessageReceived(SampleMessage message)
    {
        // ...your message handling logic...
    }
}
```
