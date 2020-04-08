---
title: Message Identifier
permalink: /docs/advanced/message-id

toc: false
---

Silverback must be able to get a unique indentifier for the integration messages in order to be able to use the exactly-once inbound or the deferred outbound connectors.

The default implementation of `IMessageIdProvider` looks for a public property called either `Id` or `MessageId` and supports `Guid` or `String` as data type.

If needed you can implement your own `IMessageIdProvider` and register it for dependency injection.

```csharp
public class SampleMessageIdProvider : IMessageIdProvider
{
    public bool CanHandle(object message) => message is SampleMessage;

    public string GetId(object message) => ((SampleMessage)message).Sequence;

    public void EnsureIdentifierIsInitialized(object message)
    {
        var sampleMessage = (SampleMessage)message;

        if (sampleMessage.Sequence = 0)
            sempleMessage.Sequence = SequenceHelper.GetNext();
    }
}
```

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddMessageIdProvider<SampleMessageIdProvider>());
    }
}
{% endhighlight %}
</figure>