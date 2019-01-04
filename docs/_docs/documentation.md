---
title:  Documentation
permalink: /docs
toc: false
---

Silverback is a simple framework to build reactive, event-driven, microservices.

Let's have a look at each of its packages, available on [nuget.org](https://www.nuget.org/packages?q=Silverback):

Package | Description
:-- | :--
Silverback.Core | It implements a very simple, yet very effective, publish/subscribe in-memory bus that can be used to decouple the software parts and easily implement a Domain Driven Design approach.
Silverback.Core.EntityFrameworkCore | Adds the ability to fire the domain events as part of the SaveChanges transaction.
Silverback.Core.Rx | Adds the possibility to create an Rx Observable over the internal bus.
Silverback.Integration | Contains the message broker and connectors abstraction. Inbound and outbound connectors can be attached to a message broker to either export some events/commands/messages to other microservices or react to the messages fired by other microservices in the same way as internal messages are handled.
Silverback.Integration.EntityFrameworkCore | Enables the creation of the inbound and outbound messages DbSet. This approach leads to fully transactional messaging, since the outbound messages are saved in the same transaction as the changes to the local data.
Silverback.Integration.Kafka | An implementation of Silverback.Integration for the popular Apache Kafka message broker. It internally uses the Confluent.Kafka client.
Silverback.Integration.Configuration | Contains the logic to read the broker endpoints configuration from the IConfiguration from Microsoft.Extensions.Configuration (appsettings.json, environment variables, etc.)

Have a look at the quickstart to see how simple it is to start working with it and how much you can achieve with very few lines of code.