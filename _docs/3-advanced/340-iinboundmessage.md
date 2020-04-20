---
title: IInboundEnvelope
permalink: /docs/advanced/iinboundenvelope

toc: false
---

When a message is consumed Silverback wraps it into an `IInboundEnvelope` and pushes it to the message bus. Both the `IInboundEnvelope` or the contained message in its pure form can be subscribed.

You can take advantage of this mechanism to gain access to the transport information of the message, since the `IInboundEnvelope` holds all this information like endpoint, offset and headers data.

Subscribing to the `IInboundEnvelope` works exactly the same as subscribing to any other message.

```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    public async Task OnWrappedMessageReceived(IInboundEnvelope<SampleMessage> envelope)
    {
        // ...your message handling logic...
    }

    public async Task OnPureMessageReceived(SampleMessage message)
    {
        // ...your message handling logic...
    }
}
```

Subscribing to the non-generic `IInboundEnvelope` or `IInboundRawEnvelope` it is possible to catch even the messages with an empty body.
{: .notice--note}
