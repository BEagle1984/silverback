---
layout: post
title: Silverback.Integration.Configuration released!
---

The first usable version of the new `Silverback.Integration.Configuration` package has been released. It allows to externalize the endpoints configuration loading it via `Microsoft.Extensions.Configuration`.

```c#
{
  "Silverback": {
    "Using": [ "Silverback.Integration.Kafka" ],
    "Inbound": [
      {
        "Endpoint": {
          "Type": "KafkaConsumerEndpoint",
          "Name": "catalog-events",
          "Configuration": {
            "BootstrapServers": "PLAINTEXT://kafka:9092",
            "ClientId": "basket-service",
            "AutoOffsetReset": "Earliest"
          }
        },
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
          }
        }
      }
    ]
  }
}
```