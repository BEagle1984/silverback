---
title:  About
permalink: /about
author_profile: true
---

Silverback is an open-source project maintained by Sergio Aquilini (aka BEagle1984).

# In few words

Silverback is a simple but feature-rich framework to build reactive/event-driven applications.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other applications or microservices. At the moment it supports [Apache Kafka](https://kafka.apache.org/) and [RabbitMQ](https://www.rabbitmq.com/) and other message brokers might be added in the future.

Its main features are:
* Simple yet powerful message bus
* Abstracted and configurative integration with a message broker
* Apache Kafka and RabbitMQ integration
* DDD, Domain Events and Transactional Messaging
* Outbox table pattern implementation
* Built-in error handling policies for consumers

# License

The code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

# Credits

Silverback uses the following libraries under the hood:
* [Rx.Net](https://github.com/dotnet/reactive)
* [Confluent's .NET Client for Apache Kafka](https://github.com/confluentinc/confluent-kafka-dotnet)

# Special Thanks

A very big thank you to my friends and colleagues:
* [Fabio](https://github.com/ppx80) for the help with Kafka
* [Laurent](https://github.com/lbovet) for constantly challenging, pushing and bringing new ideas and feedbacks
* [Marc](https://github.com/msallin) for its contributions and the valuable constant feedbacks