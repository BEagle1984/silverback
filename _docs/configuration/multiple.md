---
title: Multiple Configurations
permalink: /docs/configuration/multiple
---

There is no limit to the amount of endpoints, configurations and implementations of the inbound/outbound connectors used within a single bus or broker.

## Multiple inbound / outbound connector implementations

In the following example different inbound connectors are mixed. Silverback will simply use the first one, if the type is not explicitly specified when configuring the endpoints.

```c#
protected override void ConfigureServices(IServiceCollection services)
{
    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddDbInboundConnector<MyDbContext>()
            .AddInboundConnector());

protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider)
{
    ConfigureNLog(serviceProvider);

    endpoints
        .AddInbound(CreateConsumerEndpoint("order-events"))
        // The following inbound endpoint will use a basic InboundConnector instead of the default LoggedInboundConnector
        .AddInbound<InboundConnector>(CreateConsumerEndpoint("legacy-messages"))
        .Broker.Connect();
}
```