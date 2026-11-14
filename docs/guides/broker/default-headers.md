---
uid: default-headers
description: "Reference of the default headers Silverback adds to produced Kafka and MQTT messages, plus how to customize the default header names."
---

# Default Headers

Silverback will add some headers to the produced messages. They may vary depending on the scenario.
Here is the list of the default headers that may be sent.

The static classes <xref:Silverback.Messaging.Messages.DefaultMessageHeaders>, <xref:Silverback.Messaging.Messages.KafkaMessageHeaders> and <xref:Silverback.Messaging.Messages.MqttMessageHeaders> contain all default header names constants.

## Default

[!INCLUDE [Default headers](../../build/generated/headers-default.md)]

## Kafka

[!INCLUDE [Kafka headers](../../build/generated/headers-kafka.md)]

## MQTT

[!INCLUDE [MQTT headers](../../build/generated/headers-mqtt.md)]

## Customizing Default Header Names

The default header names can be overridden using the `WithCustomHeaderName` configuration method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .WithCustomHeaderName(DefaultMessageHeaders.ChunkIndex, "x-ch-idx")
    .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-ch-cnt")
    .AddBrokerClientsConfigurator<MyClientsConfigurator>();
```

> [!Note]
> The MQTT `x-chunk-message-id` header (<xref:Silverback.Messaging.Messages.MqttMessageHeaders>.`ChunkMessageId`) can't be renamed.
