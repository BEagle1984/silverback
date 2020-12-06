# IInboundEnvelope

When a message is consumed Silverback wraps it into an <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and pushes it to the message bus. Both the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> or the contained message in its pure form can be subscribed.

You can take advantage of this mechanism to gain access to the transport information of the message, since the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> holds all the information like endpoint, offset and headers data.

Subscribing to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> works exactly the same as subscribing to any other message.

```csharp
public class SubscribingService
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

> [!Note]
> Subscribing to the non-generic <xref:Silverback.Messaging.Messages.IInboundEnvelope> or <xref:Silverback.Messaging.Messages.IRawInboundEnvelope> it is possible to subscribe even the messages with an empty body.
