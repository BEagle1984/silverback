---
uid: message-id
---

# Message Identifier

Silverback will ensure that an `x-message-id` header is always sent with each message. This header is important not only for tracing purpose but also to enable exactly-once consuming, chunking and other features.

By default it will be initialized with a new `Guid` upon producing, unless already explicitely set either using the annotation (see example below) or through an `IBehavior`. More information about the message headers can be found in the <xref:headers> section in the quickstart.

```csharp
using Silverback.Messaging.Messages;

namespace Sample
{
    public class OrderSubmittedEvent
    {
        [Header(DefaultMessageHeaders.MessageId)]
        public string UniqueOrderNumber { get; set; }
    }
}
```

> [!Note]
> This example assumes that only one message per each order is published to the same endpoint, because the message id muse be unique.
