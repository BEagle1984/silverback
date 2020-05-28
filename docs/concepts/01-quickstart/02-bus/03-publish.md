---
uid: publishing
---

# Publishing

## Basic Publishing

To publish the message you just need an instance of `IPublisher` (or derived interfaces if using `Silverback.Core.Model`, as shown later on).

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IPublisher _publisher;

    public PublishingService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishSomething()
    {
        await _publisher.PublishAsync(new SampleMessage 
        { 
            Content = "whatever"
        });
    }
}
```

The publisher always exposes a synchronous and an asynchronous version of each method. The second option is of course to be preferred to take advantage of non-blocking async/await.

## Return values

In some cases you will of course return a respoonse after having processed the message..

```csharp
public async Task<Report> PublishSomething()
{
    var result = await _publisher.PublishAsync(new ReportQuery() { ... });

    return result.Single();
}
```

> [!Important]
> Please note the required call to `Single()`, because Silverback allows you to have multiple subscribers for the same message and therefore collect multiple return values. This is not needed if using `IQueryPublisher` or `ICommandPublisher` described in the <xref:model> page.

## Batches

It is possible to publish multiple messages with a single call to `Publish` or `PublishAsync`. The effect is very different than looping and calling the publish for each message since the collection will be handled as a batch, enabling parallel processing. It is usually suggested to publish multiple messages with the overloads accepting an `IEnumerable` and let the subscribers decide between parallel or sequential processing.

> [!Note]
> The entire batch will be processed inside the same dependency injection scope, thus allowing to handle it as a single transaction.

## Silverback.Core.Model

`Silverback.Core.Model` has been introduced in the previous page <xref:model>.

Each message type (`IEvent`, `ICommand` and `IQuery`) also comes with its specialized `IPublisher` as quickly shown in the following sub-chapters.

### Events

The messages implementing `IEvent`, `IDomainEvent` or `IIntegrationEvent` can be published using an `IEventPublisher<TEvent>` can be used to publish them.

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IEventPublisher _publisher;

    public PublishingService(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishEvent()
    {
        var myEvent = new MyEvent() { ... };
        await _publisher.PublishAsync(myEvent);
    }
}
```

### Commands

The messages that implement `ICommand`, `ICommand<TResult>` or `IIntegrationCommand` can be published using an `ICommandPublisher<TCommand>`.

# [Without result](#tab/commands-publishingservice1)
```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly ICommandPublisher _publisher;

    public PublishingService(ICommandPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ExecuteCommand()
    {
        var command = new MyCommand() { ... };
        await _publisher.ExecuteAsync(command);
    }
}
```
# [With result](#tab/commands-publishingservice2)
```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly ICommandPublisher _publisher;

    public PublishingService(ICommandPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MyResult> ExecuteCommand()
    {
        var command = new MyCommand() { ... };
        var result = await _publisher.ExecuteAsync(command);
        return result;
    }
}
```
***

### Queries

The `IQueryPublisher` ca be used  to publish the messages implementing the `IQuery<TResult>` interface.

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IQueryPublisher _publisher;

    public PublishingService(IQueryPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MyResult> GetResults()
    {
        var query = new MyQuery() { ... };
        var result = await _publisher.ExecuteAsync(myQuery);
        return result;
    }
}
```
