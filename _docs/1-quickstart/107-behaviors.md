---
title: Behaviors
permalink: /docs/quickstart/behaviors
toc: false
---

The behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting functionalities such as logging, validation, etc.

## IBehavior

A behavior must implement the `IBehavior` interface and be registered for dependency injection.

```c#
using Silverback.Messaging.Publishing;

public class ValidationBehavior : IBehavior
{
    public async Task<IEnumerable<object>> Handle(
        IEnumerable<object> messages, 
        MessagesHandler next)
    {
        foreach (var validatedMessage in messages.OfType(IValidatedMessage))
        {
            if (!validatedMessage.IsValid())
            {
                throw new InvalidMessageException();
            }
        }

        return await next(messages);
    }
}
```
```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddSilverback()
        .AddScopedBehavior<ValidationBehavior>();
```

**Note:** All `Add*Behavior` methods are available also as extensions to the `IServiceCollection` and it isn't therefore mandatory to call them immediately after `AddSilverback`.
{: .notice--info}


At every call to `IPublisher.Publish` the `Handle` method of each registered behavior is called, passing in the array of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next()` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.

### Example: Message headers

The behaviors can be quite useful to get and set the message headers for inbound/outbound messages.

```c#
public class CustomHeadersBehavior : IBehavior
{
    public async Task<IEnumerable<object>> Handle(
        IEnumerable<object> messages, 
        MessagesHandler next)
    {
        foreach (var message in messages.OfType<IOutboundMessage>())
        {
            message.Headers.Add(
                "generated-by", 
                "silverback");
            message.Headers.Add(
                "timestamp", 
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        return await next(messages);
    }
}
```
```c#
public class LogHeadersBehavior : IBehavior
{
    private readonly ILogger<LogHeadersBehavior> _logger;

    public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<object>> Handle(
        IEnumerable<object> messages, 
        MessagesHandler next)
    {
        foreach (var message in messages.OfType<IInboundMessage>())
        {
            if (message.Headers != null && message.Headers.Any())
            {
                _logger.LogInformation(
                    "Headers: {headers}",
                    string.Join(", ", 
                        message.Headers.Select(h => $"{h.Key}={h.Value}")));
            }
        }

        return await next(messages);
    }
}
```

**Note:** `IInboundMessage` and `IOutboundMessage` are internally used by Silverback to wrap the messages being sent to or received from the message broker.
{: .notice--info}

## IProducerBehavior and IConsumerBehavior

The `IProducerBehavior` and `IConsumerBehavior` are similar to the `IBehavior` but work at a lower level, much closer to the message broker. You should be able to accomplish most tasks with the normal `IBehavior`.