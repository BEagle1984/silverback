---
uid: /docs/features
---

# Features

Silverback is a simple but feature rich framework to build reactive/event-driven applications or microservices

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other applications or microservices. At the moment it supports [Apache Kafka](https://kafka.apache.org/) and [RabbitMQ](https://www.rabbitmq.com/) and other message brokers might be added in the future.

Some of its features are highlighted in the following chapters.

## Simple yet powerful message bus

Enabling the bus is as simple as referencing a nuget package and adding a single line of code to your startup class.

The provided in-memory message bus is very flexible and can be used for a multitude of use cases.

Silverback also ships with native support for Rx.net (System.Reactive).

## Message broker abstraction

The message broker integration happens configuratively at startup and it is then completely abstracted. The integration messages are published to internal bus as any other message  and Silverback takes care of deliveryng them to the correct endpoint, so that your code remains very clean and no detail about the message broker can leak into it. 

## Apache Kafka and RabbitMQ integration

Silverback provides a package to connect with the very popular [Apache Kafka](https://kafka.apache.org/) message broker or the equally popular [RabbitMQ](https://www.rabbitmq.com/).

Integrating other message brokers wouldn't be a big deal and some may be added in the future...or feel free to create your own `IBroker` implementation.

## DDD and transactional messaging

One of the main challenges when adopting a microservices architecture and asynchronous messaging is atomically updating the database and sending the messages to notify the other microservices. Silverback solves this problem for you with the built-in ability to store the messages published by your domain entities in a temporary outbox table, updated as part of your regular transaction.

Silverback integrates of seemlessly with EntityFramework Core (but could be extended to other ORMs).

## Error handling policies

Sooner or later you will run into an issue with a message that cannot be processed and you therefore have to handle the exception and decide what to do with the message.
With Silverback you can configuratively specify the error handling policies for each inbound connector. The built-in policies are:
* Skip: simply ingnore the message
* Retry: retry the same message (delays can be specified)
* Move: move the message to another topic/queue (or re-enqueue it at the end of the same one)
We believe that combining this three policies you will be able to implement pretty much all use cases.

## Distributed tracing

Silverback integrates with `System.Diagnostics` to ensure the entire flow can easily be traced, also when involving a message broker.

## Modularity

Silverback is modular and shipped in multiple nuget packages to allow you to depend only on the parts you want to use.