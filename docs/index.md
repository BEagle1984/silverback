---
title: "Home"
_layout: landing
_disableAffix: true
_disableToc: true
_disableBreadcrumb: true
_disableNextArticle: true
_disableContribution: true
---

<div class="sb-hero">
  <h1>Silverback</h1>
  <p class="sb-tagline">Simplicity at its core. Flexibility at its peak. Effortless messaging for .NET.</p>
  <p class="sb-subtitle">A message bus and broker integration library to build event-driven architectures with first-class support for <strong>Apache Kafka</strong> and <strong>MQTT</strong>.</p>
  <div class="sb-actions">
    <a class="sb-btn sb-btn-primary" href="guides/quickstart.md"><i class="bi bi-rocket-takeoff"></i> Get Started</a>
    <a class="sb-btn sb-btn-secondary" href="samples/intro.md"><i class="bi bi-collection"></i> Samples</a>
    <a class="sb-btn sb-btn-secondary" href="https://github.com/BEagle1984/silverback"><i class="bi bi-github"></i> GitHub</a>
  </div>
  <code class="sb-install">dotnet add package <span class="sb-package">Silverback.Integration.Kafka</span></code>
  <div class="sb-badges">
    <a href="https://www.nuget.org/packages/Silverback.Core"><img alt="NuGet version" src="https://img.shields.io/nuget/v/Silverback.Core?label=NuGet&color=2f6fb3"></a>
    <a href="https://www.nuget.org/packages/Silverback.Core"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/Silverback.Core?label=downloads&color=2f6fb3"></a>
    <a href="https://github.com/BEagle1984/silverback/blob/master/LICENSE"><img alt="License: MIT" src="https://img.shields.io/github/license/BEagle1984/silverback?color=2f6fb3"></a>
  </div>
</div>

## Produce and Consume in a Few Lines

Configure the broker connection, the producer and consumer endpoints, and a subscriber. Silverback takes care of serialization, headers, error handling and delivery.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<OrderCreated>(endpoint => endpoint
                .ProduceTo("orders")))
        .AddConsumer(consumer => consumer
            .WithGroupId("billing")
            .Consume<OrderCreated>(endpoint => endpoint
                .ConsumeFrom("orders")
                .OnError(policy => policy.Retry(3).ThenSkip()))))
    .AddSingletonSubscriber<OrderCreatedHandler>();
```

```csharp
// Publish: the message is routed to the "orders" topic
await publisher.PublishAsync(new OrderCreated(orderId));

// Consume: the subscriber is invoked for each consumed message
public class OrderCreatedHandler
{
    public Task HandleAsync(OrderCreated message) => ...;
}
```

## Why Silverback

<div class="sb-cards">
  <a class="sb-card" href="guides/broker/connecting.md">
    <span class="sb-card-icon"><i class="bi bi-diagram-3"></i></span>
    <span class="sb-card-title">Kafka-First, Not Kafka-Only</span>
    <p>A consistent, fluent API for Kafka and MQTT that still surfaces partitions, keys, tombstones and transactions.</p>
  </a>
  <a class="sb-card" href="guides/broker/producing/outbox.md">
    <span class="sb-card-icon"><i class="bi bi-shield-check"></i></span>
    <span class="sb-card-title">Reliable by Design</span>
    <p>Transactional outbox, error policies (retry, skip, move) and storage-backed offsets and locks.</p>
  </a>
  <a class="sb-card" href="guides/broker/consuming/batch-processing.md">
    <span class="sb-card-icon"><i class="bi bi-lightning-charge"></i></span>
    <span class="sb-card-title">Built for Throughput</span>
    <p>Partition-based parallelism, batch processing, streaming and chunking of large payloads.</p>
  </a>
  <a class="sb-card" href="guides/broker/producing/serialization.md">
    <span class="sb-card-icon"><i class="bi bi-layers"></i></span>
    <span class="sb-card-title">Cross-Cutting Features</span>
    <p>Serialization, Schema Registry (JSON, Avro, Protobuf), headers, validation and end-to-end encryption.</p>
  </a>
  <a class="sb-card" href="guides/logging.md">
    <span class="sb-card-icon"><i class="bi bi-activity"></i></span>
    <span class="sb-card-title">Observable</span>
    <p>Structured logging, distributed tracing via <code>System.Diagnostics</code> and health checks.</p>
  </a>
  <a class="sb-card" href="guides/broker/testing.md">
    <span class="sb-card-icon"><i class="bi bi-check2-circle"></i></span>
    <span class="sb-card-title">Testable</span>
    <p>In-memory Kafka and MQTT brokers and testing helpers for fast and reliable end-to-end tests.</p>
  </a>
</div>

## Explore the Documentation

<div class="sb-cards">
  <a class="sb-card" href="guides/quickstart.md">
    <span class="sb-card-title">Quickstart</span>
    <p>Produce and consume via Kafka in five minutes.</p>
  </a>
  <a class="sb-card" href="guides/setup.md">
    <span class="sb-card-title">Setting Up Silverback</span>
    <p>Packages, registration and configuration basics.</p>
  </a>
  <a class="sb-card" href="guides/bus.md">
    <span class="sb-card-title">Using the Message Bus</span>
    <p>Publish and subscribe in-process, behaviors and return values.</p>
  </a>
  <a class="sb-card" href="guides/broker/producing/basics.md">
    <span class="sb-card-title">Producing Messages</span>
    <p>Endpoints, routing, headers, outbox and Kafka specifics.</p>
  </a>
  <a class="sb-card" href="guides/broker/consuming/basics.md">
    <span class="sb-card-title">Consuming Messages</span>
    <p>Subscribers, parallelism, batches and error handling.</p>
  </a>
  <a class="sb-card" href="samples/intro.md">
    <span class="sb-card-title">Samples</span>
    <p>Runnable Kafka and MQTT examples.</p>
  </a>
  <a class="sb-card" href="guides/migration-v5.md">
    <span class="sb-card-title">Migrating from v4</span>
    <p>Step-by-step upgrade guide to Silverback 5.</p>
  </a>
  <a class="sb-card" href="guides/faq.md">
    <span class="sb-card-title">FAQ and Troubleshooting</span>
    <p>Answers to the most common questions.</p>
  </a>
  <a class="sb-card" href="api/Silverback.yml">
    <span class="sb-card-title">API Reference</span>
    <p>The complete public API.</p>
  </a>
</div>

## Supported .NET Versions

Starting with v5, Silverback targets the **latest .NET LTS** (Long-Term Support) version only. It can run on newer compatible .NET versions (including STS releases), but older frameworks are no longer targeted or supported. This policy reduces maintenance overhead and keeps Silverback aligned with the .NET platform lifecycle.

## Community

- [Issues](https://github.com/BEagle1984/silverback/issues) – report bugs or suggest improvements.
- [Discussions](https://github.com/BEagle1984/silverback/discussions) – ask questions and share ideas.
- <xref:contributing> – how to contribute.

Silverback is open source, licensed under the MIT License and maintained by Sergio Aquilini. Learn more about the project, its maintainer and the libraries it builds upon in [About Silverback](xref:about).

<div class="sb-banner">
  <i class="bi bi-info-circle"></i> Looking for the previous version? The v4 documentation is still available at <a href="/v4/">silverback-messaging.net/v4</a>. Found a mistake? Please <a href="https://github.com/BEagle1984/silverback/issues">open an issue</a>.
</div>
