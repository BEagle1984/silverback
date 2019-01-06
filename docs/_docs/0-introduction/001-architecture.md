---
title:  Architecture
permalink: /docs/architecture
---

Silverback is basically two things:
* a message bus that can be used to decouple layers or components inside an application
* an abstraction over a message broker like Apache Kafka.

Combining those two fundamental pieces allows to build reactive and resilient microservices, using a very simple and familiar programming model.

The following diagram shows the Silverback's main components and how they interact in a typical use case.

![Architecture Overview]({{ site.baseurl }}/assets/images/arch-overview.png)

\# | Description
-- | :--
1 | Some domain entities are modified
2 | When the entities are saved 1 or more events are published
3 | Some other services subscribe to such events and perform some extra work (that may lead to other events being published and so on)
4 | The outbound connector catches the messages to relay them to the message broker
5 | A producer is used to publish the messages to a Kafka topic
6 | The messages are published to Kafka
7 | Another microservices is consuming the topic and receives the messages
8 | The consumer forwards the messages to the inbound connector
9 | The messages are relayed to the internal message bus
10 | Some subscribers process the events
1b | As a result some entities are modifierd
2b | When the entities are saved 1 or more events are published
3b | The outbound connector catches the messages to relay them to the message broker
4b | A producer is used to publish the messages to a Kafka topic
5b | The messages are published to Kafka

# Packages

Silverback is modular and delivered as multiple packages, available on [nuget.org](https://www.nuget.org/packages?q=Silverback):

Package | Description
:-- | :--
Silverback.Core<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Core.svg)](https://www.nuget.org/packages/Silverback.Core/) | It implements a very simple, yet very effective, publish/subscribe in-memory bus that can be used to decouple the software parts and easily implement a Domain Driven Design approach.
Silverback.Core.EntityFrameworkCore<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/) | Adds the ability to fire the domain events as part of the SaveChanges transaction.
Silverback.Core.Rx<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Core.Rx.svg)](https://www.nuget.org/packages/Silverback.Core.Rx/) | Adds the possibility to create an Rx Observable over the internal bus.
Silverback.Integration<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.svg)](https://www.nuget.org/packages/Silverback.Integration/) | Contains the message broker and connectors abstraction. Inbound and outbound connectors can be attached to a message broker to either export some events/commands/messages to other microservices or react to the messages fired by other microservices in the same way as internal messages are handled.
Silverback.Integration.EntityFrameworkCore<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Integration.EntityFrameworkCore/) | Enables the creation of the inbound and outbound messages DbSet. This approach leads to fully transactional messaging, since the outbound messages are saved in the same transaction as the changes to the local data.
Silverback.Integration.Kafka<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.Kafka.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka/) | An implementation of Silverback.Integration for the popular Apache Kafka message broker. It internally uses the Confluent.Kafka client.
Silverback.Integration.Configuration<br/>[![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.Configuration.svg)](https://www.nuget.org/packages/Silverback.Integration.Configuration/) | Contains the logic to read the broker endpoints configuration from the IConfiguration from Microsoft.Extensions.Configuration (appsettings.json, environment variables, etc.)

# Read more

Have a look at the quickstart to see how simple it is to start working with it and how much you can achieve with very few lines of code.
