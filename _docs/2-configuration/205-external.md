---
title: External Configuration
permalink: /docs/configuration/external
toc: false
---

Alternatively the package `Silverback.Integration.Configuration` can be used to setup the endpoints from the `IConfiguration` provided by `Microsoft.Extensions.Configuration` (configuration usually coming either from the appsettings.json and/or the environment variables).

To do so the startup code has to be slightly adapted as follows.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, BusConfigurator busConfigurator))
    {
        busConfigurator.Connect(endpoints => endpoints
            .ReadConfig(Configuration, app.ApplicationServices));
    }
}
{% endhighlight %}
</figure>

And here is an example of configuration in the appsettings.json file:
```json
{
  "Silverback": {
    "Using": [ "Silverback.Integration.Kafka", "Silverback.Core.Model" ],
    "Inbound": [
      {
        "Endpoint": {
          "Type": "KafkaConsumerEndpoint",
          "Name": "catalog-events",
          "Configuration": {
            "BootstrapServers": "PLAINTEXT://kafka:9092",
            "GroupId": "basket-service",
            "ClientId": "basket-service",
            "AutoOffsetReset": "Earliest"
          },
        "Settings": {
          "Consumers": 2,
          "Batch": {
            "Size": 100,
            "MaxWaitTime": "00:00:02.500"
          }
        },        },
        "ErrorPolicies": [
          {
            "Type": "Retry",
            "MaxFailedAttempts": 5,
            "DelayIncrement": "00:00:30"
          },
          {
            "Type": "Move",
            "Endpoint": {
              "Type": "KafkaProducerEndpoint",
              "Name": "basket-failedmessages"
            }
          }
        ]
      }
    ],
    "Outbound": [
      {
        "MessageType": "IIntegrationEvent",
        "Endpoint": {
          "Type": "KafkaProducerEndpoint",
          "Name": "basket-events",
          "Configuration": {
            "BootstrapServers": "PLAINTEXT://kafka:9092",
            "ClientId": "basket-service"
          },
          "Chunk": {
              "Size": 3
          }
        }
      }
    ]
  }
}
```

This approach is discouraged and the package may even be discontinued in a future release because
not all features and all the possible configurations can be supported. The configuration will also quickly become very verbose and repetitive.
For these reasons it is advised to use the fluent API to configure Silverback.
{: .notice--warning}