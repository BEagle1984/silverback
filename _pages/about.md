---
title:  About
permalink: /about
author_profile: true
---

Silverback is an open-source project maintained by Sergio Aquilini (aka BEagle1984).

# In few words

Silverback is a simple framework to build reactive, event-driven, microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) is supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted and configurative integration with a message broker
* Apache Kafka integration
* DDD, Domain Events and Transactional Messaging
* Built-in error handling policies for consumers
* Configuration through fluent API or external configuration (`Microsoft.Extensions.Configuration`)

# License

The code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

# Credits

Silverback uses the following libraries under the hood:
* [Rx.Net](https://github.com/dotnet/reactive)
* [Confluent's .NET Client for Apache Kafka](https://github.com/confluentinc/confluent-kafka-dotnet)

# Special Thanks

A very big thank you to my friend and colleague [Fabio](https://github.com/ppx80) for the help with Kafka.