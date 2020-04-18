---
title: Introduction and Glossary
permalink: /docs/quickstart/glossary
---

Silverback is essentially a bus that can be either used internally to an application or connected to a message broker to integrate different applications or microservices.

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/overview-detailed.png"><img src="{{ site.baseurl }}/assets/images/diagrams/overview-detailed.png"></a>
    <figcaption>Silverback is used to produce the messages 1 and 3 to the message broker, while the messages 2 and 3 are also consumed locally, within the same application.</figcaption>
</figure>

## Glossary

The following list serves as introduction to the terminology and types used in Silverback.

### Publisher
An object that can be used to publish messages to the internal in-memory bus. It is accessed injecting `IPublisher` or (better) the more specific `IEventPublisher` and `ICommandPublisher` into your services.

### Subscriber
A method (or delegate) that is subscribed to the bus and will process some (or all) of the messages that will be published.

### Broker
A message broker, like Apache Kafka or RabbitMQ. It is abstracted by the `IBroker` interface and is used internally by Silverback to bind the internal bus with a message broker. It can be use directly but that shouldn't be necessary.

### Producer
An object used to publish messages to the broker. It is abstracted by the `IProducer` interface.

### Consumer
An object used to receive messages from the broker. It is abstracted by the `IConsumer` interface.

### Endpoint
Identifies a specific topic or queue. It also contains all the settings to bind to that endpoint and is therefore specific to the message broker implementation.

### Inbound Connector
Connects to an endpoint and relays the received messages to the internal bus, where they can be consumed by one or more subscribers.

### Outbound Connector
Used to relay the messages published to the internal bus to the message broker.

### Behavior
Multiple behaviors are chained to build a sort of pipeline to process the messages transiting across the internal bus, the consumer or the producer. They are used to implement cross-cutting concerns, isolate responsibilities and allow for greater flexibility. Some built-in behaviors are responbile for serialization, error policies enforcement, batch handling, encryption, etc.
