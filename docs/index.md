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
                        <span style="font-size: .8em;">Simplicity at its core.</span><br />
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

> [!Important]
> The old v4 documentation is still browsable here: [silverback-messaging.net/v4](/v4)

## What is Silverback?

Silverback is a **powerful, elegant, and feature-rich message bus for .NET**, designed to simplify asynchronous messaging, event-driven architectures, and microservice communication. With seamless integration for **Apache Kafka** and **MQTT**, it offers robust features for **reliability, consistency, and scalability**.

Whether you're building a **small microservice, a large-scale enterprise solution, or anything in between**, Silverback provides the tools to make messaging effortless and reliable.

## Why Choose Silverback?

Silverback is designed for **serious workloads**, offering enterprise-grade capabilities with a rich feature set optimized for **performance, resilience, and scalability**.

### Key Features

🔀 **Powerful Mediator**\
Enables seamless in-memory communication between components, featuring **Rx.NET** support for reactive programming.

🚀 **Message Broker Abstraction**\
Easily integrate with **Kafka** or **MQTT** to build event-driven architectures, distributed workflows, and scalable inter-service communication with minimal setup.

📤 **Transactional Outbox**\
Ensures message consistency by linking database transactions with messaging, preventing message loss and guaranteeing atomicity.

⚠️ **Advanced Error Handling**\
Define flexible strategies to **retry, skip, or move messages** based on custom policies, ensuring robustness in failure scenarios.

📦 **Batch Processing & Chunking**\
Enhances efficiency by processing messages in bulk or splitting large messages into smaller chunks, which are automatically reassembled on the receiving end.

⚡ **Domain-Driven Design (DDD) Support**\
Automates domain event publishing when entities are persisted, ensuring seamless integration with message brokers for event-driven workflows.

✅ **Exactly-Once Processing**\
Ensures each message is consumed and processed exactly once, preventing duplicate processing and maintaining data integrity.

🔍 **Distributed Tracing**\
Leverages **System.Diagnostics** for full visibility into message flow and distributed transaction tracking.

🧪 **Testability**\
Provides in-memory mocks for **Kafka** and **MQTT**, along with powerful helpers for efficient unit testing.

✨ **And much more!**\
Silverback is highly extensible, making it the go-to messaging framework for .NET developers.

## Getting Started

### Learn the Basics

Silverback is designed to be intuitive, but a solid foundation makes it even easier to use. Explore:

- 📖 **[Guides](guides/setup.html)** – Learn the core principles and architecture.
- 🛠 **[Examples](examples/examples.html)** – Hands-on examples to see Silverback in action.
- 📚 **[API Reference](api/Silverback.html)** – Dive into the detailed API documentation.

### Install via NuGet

Getting started is as easy as installing the necessary packages for your use case. Silverback is modular and consists of multiple packages, such as **Silverback.Core**, **Silverback.Integration.Kafka**, and **Silverback.Integration.Mqtt**.\
More details about the different packages can be found in the **[Setting Up Silverback](guides/setup.html)** guide.

## Join the Silverback Community

Silverback is **open-source** and thrives thanks to **contributors like you**! Whether it's bug reports, feature suggestions, or pull requests, we welcome your support.

- 💡 **[Contribute](contributing.md)** – Discover how you can help improve Silverback.
- 💬 **[Discussions](https://github.com/BEagle1984/silverback/discussions)** – Join the conversation and share your insights.

A huge **thank you** to all contributors who help make Silverback even better!



</div>
<div style="height: 100px"></div>
