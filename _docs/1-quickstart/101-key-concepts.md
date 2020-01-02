---
title: Key Concepts
permalink: /docs/quickstart/key-concepts
toc: false
---

The following list serves as introduction to the terminology and types used in Silverback.

* **Publisher**: an object that can be used to publish messages to the internal in-memory bus. It is accessed injecting `IPublisher` or (better) the more specific `IEventPublisher` and `ICommandPublisher` into your services.
* **Subscriber**: a method (or delegate) that is subscribed to the bus and will process some (or all) of the messages that will be published.
* **Broker**: a message broker, like Apache Kafka or RabbitMQ. It is abstracted by the `IBroker` interface and is used internally by Silverback to bind the internal bus with a message broker. It can be use directly but that shouldn't be necessary.
* **Producer**: an object used to publish messages to the broker. It is abstracted by the `IProducer` interface.
* **Consumer**: an object used to receive messages from the broker. It is abstracted by the `IConsumer` interface.
* **Endpoint**: identifies a specific topic or queue. It also contains all the settings to bind to that endpoint and is therefore specific to the message broker implementation.
* **Inbound Connector**: connects to an endpoint and relays the received messages to the internal bus, where they can be consumed by one or more subscribers.
* **Outbound Connector**: used to relay the messages (implementing `IIntegrationMessage`) published to the internal bus to the message broker.
