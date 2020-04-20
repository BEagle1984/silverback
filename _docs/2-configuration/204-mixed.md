---
title: Mixed Configurations
permalink: /docs/configuration/mixed
toc: false
---

There is no limit to the amount of endpoints, configurations and implementations of the inbound/outbound connectors used within a single bus or broker.

In the following example different outbound connectors are mixed. Silverback will simply use the first one by default, unless the type is not explicitly specified when configuring the endpoints.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                // Use the DeferredOutboundConnector by default
                .AddDeferredOutboundConnector()
                // ...but register the simple OutboundConnector as well
                .AddOutboundConnector());
    }

    public void Configure(BusConfigurator busConfigurator)
    {
        ConfigureNLog(serviceProvider);

        busConfigurator
            .Connect(endpoints => endpoints
                // This endpoint will use DeferredOutboundConnector
                .AddOutbound<IEvent>(new KafkaConsumerEndpoint("order-events")
                {
                    ...
                })
                // ...and this endpoint will use the simple OutboundConnector instead
                .AddOutbound<SomeCommand, OutboundConnector>(new KafkaConsumerEndpoint("some-commands")
                {
                    ...
                }));
    }
}
{% endhighlight %}
</figure>