---
title: Features
permalink: /docs/features

excerpt: Silverback is a simple framework to build reactive, event-driven, microservices.

toc: true
toc_label: Features
toc_icon: star
---

Silverback is a simple framework to build reactive, event-driven, microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other microservices. At the moment only Apache Kafka is supported but other message brokers could be added without much effort.

Some of its features are highlighted in the following chapters.

# Simple yet powerful message bus

Enabling the bus is as simple as adding a nuget package and a putting a single line of code in your startup class and with just a few lines of code you can setup an in-memory message bus to cover a multitude of use cases.

It is based on the publish/subscribe pattern and the implementation relies on the .net core built-in dependency injection.

# Message broker abstraction

The message broker integration happens configuratively at startup and it is then completely abstracted. The integration messages are simply published to internal bus or relayed from the internal but to the message broker, so that your code remains very clean and no detail about the message broker can leak into it. 

# Apache Kafka integration

Silverback provides a package to connect with the very popular [Apache Kafka](https://kafka.apache.org/) message broker.

Integrating other message brokers wouldn't be a big deal and some may be added in the future...or feel free to create your own `IBroker` implementation.

# DDD and transactional messaging

One of the main challenges when adopting a microservices architecture based on asnchronous messaging is atomically updating the database and sending the messages to notify the other microservices. Silverback solves this problem for you with the built-in ability to store the messages published by your domain entities in a temporary outbox database table, updated as part of your regular transaction.

# Error handling policies

You can configuratively specify the error handling policies for each inbound connector. The built-in policies are:
* Skip: simply ingnore the message
* Retry: retry the same message (delays can be specified)
* Move: move the message to another topic/queue (or re-enqueue it at the end of the same one)
We believe that combining this three policies you will be able to implement pretty much all use cases.

# External configuration

The message broker connected endpoints can be configured through a fluent API or externalized through `Microsoft.Extensions.Configuration`, supporting of course all usual providers (appsettings.json, environment variables, ...).

# Modularity

Silverback is splitted into multiple packages to allow you to depend only on the parts you plan to use.