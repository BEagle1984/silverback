---
documentType: index
title: "Home"
---

<div role="main" class="hide-when-search">
    <div style="background-color: #000;">
        <div class="container body-container">
            <div class="hero" style="background-image: url('images/splash.jpg');">
                <div class="wrapper">
                    <h1 id="page-title" class="page-title" itemprop="headline">        
                        Silverback
                    </h1>
                    <p class="lead">
                        <span style="font-size: .9em;">Simplicity at its core.</span><br />
                        <span style="font-size: .9em;">Flexibility at its peak.</span><br />
                        Effortless messaging for .NET
                    </p>
                    <p>
                        <a href="https://github.com/BEagle1984/silverback/" class="btn"><i class="fab fa-github"></i> View on GitHub</a>
                        <a href="https://www.nuget.org/packages?q=Silverback" class="btn"><i class="fas fa-arrow-alt-circle-down"></i> Get from NuGet</a>
                    </p>
                </div>
            </div>
        </div>
    </div>
</div>
<div class="container body-container body-content">

> [!Tip]
> The v4 documentation is still browsable here: [silverback-messaging.net/v4](/v4)

> [!Important]
> The documentation update for v5 is still in progress. Some sections may be missing or incomplete. Please notify us if you find any mistake or inconsistency.

Silverback is a message bus and broker integration library for .NET.
It is designed to simplify event-driven architectures and asynchronous messaging, with first-class support for **Apache Kafka** and **MQTT**.

Silverback aims to be both **high-level** (consistent configuration and developer experience) and **broker-aware**.
Kafka is a first-class citizen: features like partition-based parallelism, keys/partitioning, tombstones, Schema Registry integration, idempotency, and transactions are surfaced where they matter.

## Why Silverback

- **Kafka-first, not Kafka-only** – a consistent API across brokers, while still leveraging Kafka-specific capabilities.
- **Reliable by design** – transactional outbox, error policies, and storage-backed features.
- **Operational usability** – structured logging, diagnostics, and tracing.
- **Built-in cross-cutting features** – headers, validation, encryption, chunking, batching.
- **Testability** – in-memory broker mocks and end-to-end helpers.

## Key Features

- **In-memory message bus** with optional Rx.NET integration.
- **Broker integration** for Kafka and MQTT with a consistent, fluent configuration API.
- **Kafka-optimized**: partition-based parallelism, keys/partitioning, tombstones, Schema Registry integration, idempotency, and transactions.
- **Transactional outbox** and storage-backed features.
- **Error policies** to retry, skip, or move messages.
- **Batch processing** and **chunking** for throughput and large payloads.
- **Distributed tracing** via `System.Diagnostics`.
- **Testing support** with in-memory brokers and helper APIs.

## Supported .NET Versions

Starting with v5, Silverback targets the **latest .NET LTS** (Long-Term Support) version only.

The library is built and tested against the current LTS release.
It can run on newer compatible .NET versions (including STS releases), but older frameworks are no longer targeted or supported.

This policy reduces maintenance overhead and keeps Silverback aligned with the .NET platform lifecycle.

## Getting Started

- <xref:setup> – core concepts and configuration.
- <xref:samples> – runnable examples.
- [API Reference](xref:Silverback) – full API documentation.

## Community

- [Issues](https://github.com/BEagle1984/silverback/issues) – report bugs or suggest improvements.
- [Discussions](https://github.com/BEagle1984/silverback/discussions) – ask questions and share ideas.
- <xref:contributing> – how to contribute.

</div>
<div style="height: 100px"></div>
