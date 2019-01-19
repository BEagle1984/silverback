---
title: Model
permalink: /docs/quickstart/model
---

A hierarchy of interfaces is available in _Silverback.Core.Model_ to help specify the meaning of each message and produce in better, cleaner and more readable code.

The _internal messages_ are being sent through the internal in-memory bus and don't leave the service scope, while the _integration messages_ are those messages exchanged between different microservices, through a message broker like Apache Kafka.

Event though strongly suggested, it's not mandatory to use the proposed hierarchy from _Silverback.Core.Model_ and everything can be achieved using POCO classes as messages and using the generic `IPublisher` to publish them.

In the following chapters you will find an overview of the different message types and their meaning.

## Internal Messages

This messages can be used internally to the microservice bus but cannot be relayed to the message broker. (See [Translating Message]({{ site.baseurl }}/docs/quickstart/translating) for a convenient way to map the internal message to an `IIntegrationMessage`.)

### Events

`IEvent` is to be used to notify thing that happened inside a service and may be of some interest for one or more other service. The events are a fire-and-forget message type and no response is expected.
`IEventPublisher<TEvent>` can be injected to publish them.

```c#
private readonly IEventPublisher _publisher;

public async Task PublishEvent()
{
   var myEvent = new MyEvent() { ... };
   await _publisher.PublishAsync(myEvent);
}
```

`IDomainEvent` extends `IEvent` and should be published only from within the domain entities (actually adding them to the internal collection and letting them be published during the save changes transaction).

### Commands

`ICommand` or `ICommand<TResult>` are used to trigger an action in another service or component and are therefore very specific and usually consumed by one single service. This messages can return a value (TResult).
`ICommandPublisher<TCommand>` can be injected to publish them.

```c#
private readonly ICommandPublisher _publisher;

public async Task ExecuteCommand()
{
   var command = new MyCommand() { ... };
   await _publisher.ExecuteAsync(command);
}
```
```c#
private readonly ICommandPublisher _publisher;

public async Task<MyResult> ExecuteCommand()
{
   var command = new MyCommand() { ... };
   var results = await _publisher.ExecuteAsync(command);
   return results.Single();
}
```

### Queries

`IQuery<TResult>` works exactly like `ICommand<TResult>`. This messages are obviously always returning something.

```c#
private readonly IQueryPublisher _publisher;

public async Task<MyResult> DoWork()
{
   var query = new MyQuery() { ... };
   var results = await _publisher.ExecuteAsync(myQuery);
   return results.SingleOrDefault();
}
```

## Integration messages

The `IIntegrationMessage` interface identifies those messages that are either published to the message broker or received through it (Note that `IIntegrationMessage` implements `IMessage`, obviously). 

### Integration Event

`IIntegrationEvent` can be used to export events to other microservices or, more generally, other systems.

`IEventPublisher` can be used to publish these events and they will automatically be routed to the message broker if an outbound connector was properly configured. See [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker) for details.

### Integration Command

`IIntegrationCommand` is used to trigger an action on another microservices (or systems).

`ICommandPublisher` can be used to publish these messages and they will automatically be routed to the message broker if an outbound connector was properly configured. See [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker) for details.