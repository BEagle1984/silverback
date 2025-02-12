---
uid: translating-messages
---

# Translating Messages

It is not uncommon to be willing to slightly transform the internal message and maybe publish only a subset of the information to the message broker (e.g. you may not want to export the full entity related to the domain event). You can easily achieve this with a subscriber that just maps/translates the messages.

```csharp
public class MapperService
{
    public IMessage MapCheckoutEvent(CheckoutDomainEvent message) => 
        new CheckoutIntegrationEvent
        {
            UserId = message.Source.UserId,
            Total = mesage.Source.Total,
            ...
        };
}
```

As explained in the <xref:publish> section, when the subscriber returns one or more messages those are automatically republished to the bus. The message is then relayed to the message broker, if its type matches with an outbound endpoint declaration.
