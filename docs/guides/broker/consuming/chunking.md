---
uid: consuming-chunking
description: "Consume chunked messages in Silverback: the consumer automatically reassembles the chunks split by the producer before delivering them to subscribers."
---

# Consuming Chunked Messages

When messages are split into smaller parts (chunks) by the producer, the consumer is responsible for reassembling them into the original message before delivering it to the subscribers. Silverback handles chunked messages automatically, so in most cases you don't need to write any special consumer-side logic (if the messages are produced using Silverback or another framework that adds compatible headers).

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png" alt="Chunking diagram" /></a>
    <figcaption>The messages are being split into small chunks and reassembled on the consumer side.</figcaption>
</figure>

## Consumer Configuration

No special configuration is required on the consumer to support chunked messages.

> [!Note]
> All chunks belonging to the same message are acknowledged/committed at once, when the full message has been reassembled and processed by the subscriber. Similarly, when an exception is thrown, the error policy is applied to all chunks.

> [!Important]
> The chunks belonging to the same message are expected to be contiguous and in strict order. When using Kafka, the chunks belonging to the same message are also supposed to be in the same partition.

### MQTT

Chunking requires MQTT 5.

- The chunks must be contiguous in all the messages consumed by the client, not only in their topic. A chunk of another message received between the chunks, from any topic or endpoint of the client, discards the incomplete message (that is acknowledged and lost), and so does any other message. Consume the chunked messages with a dedicated client, subscribed only to their topic, and produce them with a single producer.
- The chunks can't be consumed through a shared subscription, since the broker would spread them across the consumers.
- Chunking can't be combined with batch processing (see <xref:batch-processing>). A client with batch processing rejects each chunk with a <xref:Silverback.Messaging.Sequences.SequenceException>, which the error policy of the endpoint handles (e.g. skipping the chunks or moving them to another topic). The messages fitting in a single chunk aren't affected.
- With parallel processing, all the chunks of a message are processed by the same parallel execution.
- With QoS 1 or 2, the chunks are acknowledged only when the whole message is processed, and the broker stops delivering when too many messages are waiting for acknowledgment. The number of chunks of a message must not exceed the `ReceiveMaximum` of the client (see `LimitUnacknowledgedPublications`) and the limit enforced by the broker. Otherwise, the consumer stalls until the sequence timeout elapses and then discards the message.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producing-chunking> guide
- <xref:default-headers> guide
