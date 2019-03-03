---
title: Behaviors
permalink: /docs/quickstart/behaviors
toc: false
---

Behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting functionalities such as logging, validation, etc.

A behavior must implement the `IBehavior` interface and be registered for dependency injection.

```c#
using Silverback.Messaging.Subscribers;

public class ValidationBehavior : IBehavior
{
    public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
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
        .AddBus()
        .AddScoped<IBehavior, ValidationBehavior>();
```

At every call to `IPublisher.Publish` the `Handle` method of each registered behavior is called, passing in the array of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next()` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.