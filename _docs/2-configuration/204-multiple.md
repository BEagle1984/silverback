---
title: Multiple Configurations
permalink: /docs/configuration/multiple
toc: false
---

There is no limit to the amount of endpoints, configurations and implementations of the inbound/outbound connectors used within a single bus or broker.

In the following example different inbound connectors are mixed. Silverback will simply use the first one, if the type is not explicitly specified when configuring the endpoints.

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
                .AddDbInboundConnector()
                .AddInboundConnector());
    }

    public void Configure(BusConfigurator busConfigurator)
    {
        ConfigureNLog(serviceProvider);

        busConfigurator
            .Connect(endpoints => endpoints
                .AddInbound(CreateConsumerEndpoint("order-events"))
                // The following inbound endpoint will use a basic 
                // InboundConnector instead of the default
                // LoggedInboundConnector
                .AddInbound<InboundConnector>(
                    CreateConsumerEndpoint("legacy-messages")));
    }
}
{% endhighlight %}
</figure>