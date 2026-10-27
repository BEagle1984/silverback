---
uid: samples
title: Samples
---

# Samples

Short, runnable examples that demonstrate core Silverback concepts and common integration scenarios. Each sample consists of a producer and a consumer application (plus a common project, where the message contract is shared) and focuses on a single feature.

The source code of all samples is available in the [samples folder](https://github.com/BEagle1984/silverback/tree/master/samples) of the repository, grouped into the `Silverback.Samples.sln` solution.

## Running the Samples

### Prerequisites

- The .NET SDK version pinned in the repository's `global.json`.
- Docker, to run the message brokers locally.

### Starting the Infrastructure

The `docker-compose.yaml` file in the repository root starts everything the samples need: a two-node Kafka cluster (reachable at `localhost:19092` and `localhost:29092`), the Confluent schema registry (`localhost:8081`) and an EMQX MQTT cluster behind HAProxy (`localhost:1883`).

```bash
docker compose up -d
```

> [!Tip]
> The compose file also starts some web UIs that help to inspect what's going on, like Conduktor for Kafka (http://localhost:8080) and the EMQX dashboard (http://localhost:18083). The credentials are listed in the comments at the top of the compose file.

### Running a Sample

Run the producer and the consumer in two separate terminals, from the repository root. For example, for the basic Kafka sample:

```bash
dotnet run --project ./samples/Kafka/Basic.Producer/.
dotnet run --project ./samples/Kafka/Basic.Consumer/.
```

Then observe the console output while the messages are produced and consumed. Each sample page lists the exact commands and any additional step.

## Kafka Samples

<div class="sb-cards">
  <a class="sb-card" href="kafka/basic.md">
    <span class="sb-card-icon"><i class="bi bi-send"></i></span>
    <span class="sb-card-title">Kafka – Basic</span>
    <p>The simplest possible producer and consumer, with an error policy.</p>
  </a>
  <a class="sb-card" href="kafka/batch.md">
    <span class="sb-card-icon"><i class="bi bi-collection"></i></span>
    <span class="sb-card-title">Kafka – Batch Processing</span>
    <p>Publish messages in batch and consume them in batches of up to 100 messages.</p>
  </a>
  <a class="sb-card" href="kafka/batch-tombstone.md">
    <span class="sb-card-icon"><i class="bi bi-trash3"></i></span>
    <span class="sb-card-title">Kafka – Batch with Tombstones</span>
    <p>Produce tombstones within a batch and tell them apart while consuming in batch.</p>
  </a>
  <a class="sb-card" href="kafka/binaryfile-streaming.md">
    <span class="sb-card-icon"><i class="bi bi-file-earmark-binary"></i></span>
    <span class="sb-card-title">Kafka – File Streaming</span>
    <p>Stream large binary files through Kafka, split into chunks.</p>
  </a>
  <a class="sb-card" href="kafka/avro.md">
    <span class="sb-card-icon"><i class="bi bi-file-earmark-code"></i></span>
    <span class="sb-card-title">Kafka – Avro</span>
    <p>Serialize the messages as Avro using the Confluent schema registry.</p>
  </a>
  <a class="sb-card" href="kafka/json-schema-registry.md">
    <span class="sb-card-icon"><i class="bi bi-braces"></i></span>
    <span class="sb-card-title">Kafka – JSON with Schema Registry</span>
    <p>Serialize the messages as JSON, validated against a schema in the schema registry.</p>
  </a>
  <a class="sb-card" href="kafka/transactional-producer.md">
    <span class="sb-card-icon"><i class="bi bi-check2-all"></i></span>
    <span class="sb-card-title">Kafka – Transactional Producer</span>
    <p>Produce messages atomically with Kafka transactions, committing or aborting them.</p>
  </a>
</div>

## MQTT Samples

<div class="sb-cards">
  <a class="sb-card" href="mqtt/basic.md">
    <span class="sb-card-icon"><i class="bi bi-broadcast"></i></span>
    <span class="sb-card-title">MQTT – Basic</span>
    <p>The simplest possible producer and consumer, with a last will message.</p>
  </a>
  <a class="sb-card" href="mqtt/basic-v3.md">
    <span class="sb-card-icon"><i class="bi bi-broadcast-pin"></i></span>
    <span class="sb-card-title">MQTT – Basic (MQTT v3)</span>
    <p>The same basic producer and consumer, using MQTT protocol version 3.1.0.</p>
  </a>
  <a class="sb-card" href="mqtt/binaryfile-streaming.md">
    <span class="sb-card-icon"><i class="bi bi-file-earmark-arrow-up"></i></span>
    <span class="sb-card-title">MQTT – File Streaming</span>
    <p>Stream binary files through MQTT, with a custom header carrying the file name.</p>
  </a>
</div>
