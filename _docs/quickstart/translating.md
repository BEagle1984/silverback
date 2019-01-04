---
title: Translating Messages
permalink: /docs/quickstart/translating
toc: false
---

The Silverback framework can publish only messages implementing `IIntegrationMessage` to the message broker. This is meant to let you easily define and determine which messages are to be exported.

It is furthermore not uncommon to be willing to slightly transform the internal message before exporting it to the outside world (e.g. you may not want to export the full entity related to the domain event). You can easily achieve this with a subscribed method that returns a new `IIntegrationMessage`.

```c#
[Subscribe]
IMessage MapCheckoutEvent(CheckoutDomainEvent message) => new CheckoutIntegrationEvent
{
    UserId = message.Source.UserId,
    Total = mesage.Source.Total,
    ...
};
```