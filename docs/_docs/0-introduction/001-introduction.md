---
title:  Introduction
permalink: /docs/introduction
---

## What's Silverback?

Silverback is basically two things:
* a message bus (actually a mediator) that can be used to decouple layers or components inside an application
* an abstraction over a message broker like Apache Kafka or RabbitMQ.

Combining those two fundamental pieces allows to build reactive and resilient microservices, using a very simple and familiar programming model.

<figure>
	<a href="{{ site.baseurl }}/assets/images/diagrams/overview.png"><img src="{{ site.baseurl }}/assets/images/diagrams/overview.png"></a>
	<figcaption>Silverback is used to produce the messages 1 and 3 to the message broker, while the messages 2 and 3 are also consumed locally, within the same application.</figcaption>
</figure>

## Packages

Silverback is modular and delivered in multiple packages, available through [nuget.org](https://www.nuget.org/packages?q=Silverback).

### Core

#### Silverback.Core

It implements a very simple, yet very effective, publish/subscribe in-memory bus that can be used to decouple the software parts and easily implement a Domain Driven Design approach.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.svg)](https://www.nuget.org/packages/Silverback.Core/)
[![NuGet](https://img.shields.io/nuget/dt/Silverback.Core.svg)](https://www.nuget.org/packages/Silverback.Core/)

#### Silverback.Core.Model

It contains some interfaces that will help organize the messages and write cleaner code, adding some semantic. It also includes a sample implementation of a base class for your domain entities.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Model.svg)](https://www.nuget.org/packages/Silverback.Core.Model/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Core.Model.svg)](https://www.nuget.org/packages/Silverback.Core.Model/)

#### Silverback.Core.EntityFrameworkCore

It contains the storage implementation to integrate Silverback with Entity Framework Core. It is needed to use a `DbContext` as storage for (temporary) data and to fire the domain events as part of the `SaveChanges` transaction.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/)

#### Silverback.Core.Rx

Adds the possibility to create an Rx `Observable` over the internal bus.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Rx.svg)](https://www.nuget.org/packages/Silverback.Core.Rx/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Core.Rx.svg)](https://www.nuget.org/packages/Silverback.Core.Rx/)

### Integration

#### Silverback.Integration

Contains the message broker and connectors abstraction. Inbound and outbound connectors can be attached to a message broker to either export some events/commands/messages to other microservices or react to the messages fired by other microservices in the same way as internal messages are handled.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.svg)](https://www.nuget.org/packages/Silverback.Integration/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.svg)](https://www.nuget.org/packages/Silverback.Integration/)

#### Silverback.Integration.Kafka

An implementation of `Silverback.Integration` for the popular Apache Kafka message broker. It internally uses the `Confluent.Kafka` client library.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Kafka.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.Kafka.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka/)

#### Silverback.Integration.Kafka.SchemaRegistry

Adds the support for Apache Avro and the schema registry on top of `Silverback.Integration.Kafka`.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Kafka.SchemaRegistry.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.Kafka.SchemaRegistry.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/)

#### Silverback.Integration.RabbitMQ

An implementation of `Silverback.Integration` for the popular RabbitMQ message broker. It internally uses the `RabbitMQ.Client` library.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.RabbitMQ.svg)](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.RabbitMQ.svg)](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ/)

#### Silverback.Integration.InMemory

Includes a mocked message broker to be used for testing only.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.InMemory.svg)](https://www.nuget.org/packages/Silverback.Integration.InMemory/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.InMemory.svg)](https://www.nuget.org/packages/Silverback.Integration.InMemory/)

#### Silverback.Integration.HealthChecks

Contains the extensions for `Microsoft.Extensions.Diagnostics.HealthChecks` to monitor the connection to the message broker.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.HealthChecks.svg)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.Integration.HealthChecks.svg)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/)

### Event Sourcing

#### Silverback.EventSourcing

Contains an implementation of an event store that perfectly integrates within the Silverback ecosystem.

[![NuGet](http://img.shields.io/nuget/vpre/Silverback.EventSourcing.svg)](https://www.nuget.org/packages/Silverback.EventSourcing/)
[![NuGet](http://img.shields.io/nuget/dt/Silverback.EventSourcing.svg)](https://www.nuget.org/packages/Silverback.EventSourcing/)

## Read more

Have a look at the [quickstart]({{ site.baseurl }}/docs/quickstart) to see how simple it is to start working with it and how much you can achieve with very few lines of code.