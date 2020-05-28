---
uid: model
---

# Creating the Message model

## Basics

First of all we need to create a message class. The message class can be any POCO class, it just need to be serializable.

```csharp
using Silverback.Messaging.Messages;

public class SampleMessage
{
    public string Content { get; set; }
}
```

It is very much suggested to consider using the `Silverback.Core.Model` package (documented in the next chapter) to better organize your message and write better and more readable code.

## Silverback.Core.Model

A hierarchy of interfaces is available in `Silverback.Core.Model` to help specify the meaning of each message and produce in better, cleaner and more readable code.

The _internal messages_ are being sent through the internal in-memory bus and don't leave the service scope, while the _integration messages_ are those messages exchanged between different microservices, through a message broker like Apache Kafka or RabbitMQ.

Event though strongly suggested, it's not mandatory to use the proposed hierarchy from `Silverback.Core.Model` and everything can be achieved using POCO classes as messages and using the generic `IPublisher` to publish them.

In the following chapters you will find an overview of the different message types and their meaning but first of all we need to refernce the `Siverback.Core.Model` package and register it with the dependency injection.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilverback().UseModel();
    }
}
```

### Internal Messages

This messages can be used internally to the microservice bus but cannot be relayed to the message broker. See <xref: translating-messages> for a convenient way to map the internal message to an `IIntegrationMessage`.

#### Events

`IEvent` is to be used to notify thing that happened inside a service and may be of some interest for one or more other service. The events are a fire-and-forget message type and no response is expected.

The `IDomainEvent` extends `IEvent` and the types implementing this interface are usually published only from within the domain entities (actually adding them to the internal collection and letting them be published during the save changes transaction). See also <xref:domain-events>.

#### Commands

`ICommand` or `ICommand<TResult>` are used to trigger an action in another service or component and are therefore very specific and usually consumed by one single subscriber. This messages can return a value (`TResult`).

#### Queries

`IQuery<TResult>` works exactly like `ICommand<TResult>`. This messages are obviously always returning something since they represent a request for data (query).

### Integration messages

The `IIntegrationMessage` interface identifies those messages that are either published to the message broker or received through it (Note that `IIntegrationMessage` implements `IMessage`, obviously). 

#### Integration Event

`IIntegrationEvent` can be used to export events to other microservices or, more generally, other applications.

`IEventPublisher` can be used to publish these events and they will automatically be routed to the message broker if an outbound connector was properly configured. See <xref:message-broker> for details.

#### Integration Command

`IIntegrationCommand` is used to trigger an action on another microservices (or application).

`ICommandPublisher` can be used to publish these messages and they will automatically be routed to the message broker if an outbound connector was properly configured. See <xref:message-broker> for details.