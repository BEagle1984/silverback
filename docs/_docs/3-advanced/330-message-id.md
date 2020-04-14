---
title: Message Identifier
permalink: /docs/advanced/message-id

toc: false
---

Silverback will ensure that an `x-message-id` header is always sent with each message. This header is important not only for tracing purpose but also to enable exactly-once consuming, chunking and other features.

By default it will be initialized with a new `Guid` upon producing, unless already set (see the [Message Headers]({{ site.baseurl }}/docs/quickstart/headers) section in the quick start to see how to set an header).

## IMessageIdProvider

An `IMessageIdProvider` is used to automatically initialize the message model with the message id that is later published as header.

The default implementation of `IMessageIdProvider` looks for a public property called either `Id` or `MessageId` and supports `Guid` or `String` as data type.

If needed you can implement your own `IMessageIdProvider` and register it for dependency injection.

```csharp
public class SampleMessageIdProvider : IMessageIdProvider
{
    public bool CanHandle(object message) => message is SampleMessage;

    public string EnsureIdentifierIsInitialized(object message)
    {
        var sampleMessage = (SampleMessage)message;

        if (sampleMessage.Sequence = 0)
            sempleMessage.Sequence = SequenceHelper.GetNext();

        return sampleMessage.Sequence.ToString();
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

This feature will probably be removed in one of the next releases. It is advised to use an `IBehavior` to accomplish this kind of task, if still necessary.
{: .notice--warning}
