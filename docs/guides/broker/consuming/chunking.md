---
uid: consuming-chunking
description: "Consume chunked messages in Silverback: the consumer automatically reassembles the chunks split by the producer before delivering them to subscribers."
---

# Consuming Chunked Messages

When messages are split into smaller parts (chunks) by the producer, the consumer is responsible for reassembling them into the original message before delivering it to the subscribers. Silverback handles chunked messages automatically, so in most cases you don't need to write any special consumer-side logic (if the messages are produced using Silverback or another framework that adds compatible headers).

<figure>
	<a href="~/images/diagrams/chunk-basic.svg"><img src="~/images/diagrams/chunk-basic.svg" alt="Chunking diagram" /></a>
    <figcaption>The messages are being split into small chunks and reassembled on the consumer side.</figcaption>
</figure>

## Consumer Configuration

No special configuration is required on the consumer to support chunked messages.

> [!Note]
> All chunks belonging to the same message are acknowledged/committed at once, when the full message has been reassembled and processed by the subscriber. Similarly, when an exception is thrown, the error policy is applied to all chunks.

> [!Important]
> The chunks belonging to the same message are expected to be contiguous and in strict order. When using Kafka, the chunks belonging to the same message are also supposed to be in the same partition.

> [!Important]
> **MQTT Notes**
>
> - Chunking requires MQTT 5.
> - The chunks must be contiguous in all the messages consumed by the client, not only in their topic: any other message received between the chunks, from any topic of the client, discards the incomplete message, which is acknowledged and lost. Consume the chunked messages with a dedicated client, subscribed only to their topic, and not through a shared subscription, which spreads them across the consumers.
> - Chunking can't be combined with batch processing (see <xref:batch-processing>). A client with batch processing rejects each chunk with a <xref:Silverback.Messaging.Sequences.SequenceException>, which the error policy of the endpoint handles separately for each chunk (e.g. skipping the chunks, with a [callback](xref:error-policies#callbacks) to keep track of them). The messages fitting in a single chunk aren't affected.
> - With QoS 1 or 2, the broker stops delivering when too many messages are waiting for acknowledgment. The number of chunks of a message must therefore not exceed the `ReceiveMaximum` of the client (see `LimitUnacknowledgedPublications` in [Other Connection Settings](xref:mqtt#other-connection-settings)) and the limit enforced by the broker. Otherwise, the consumer stalls until the sequence timeout elapses and then discards the message.

## Chunked Messages in Batches

With Kafka, the chunked messages can be consumed with batch processing (see <xref:batch-processing>): the chunks are reassembled and the message is added to the batch. A <xref:Silverback.Messaging.Messages.BinaryMessage> is added to the batch with its first chunk, and its `Content` stream receives the next chunks while the subscriber reads it. If the subscriber moves on without reading the `Content` to the end, the binary message counts as processed once the batch completes: the batch is committed and the remaining chunks are skipped as an incomplete message, logging a warning.

> [!Warning]
> Without a max wait time, a batch whose subscriber doesn't read a chunked binary message to the end stalls, unless the message is the last of the batch: the next chunk waits to be read, so the next messages of the partition can't be added to the batch and, with `ProcessAllPartitionsTogether`, no message of any partition is processed. The stall lasts until the sequence timeout of the binary message elapses (see `WithSequenceTimeout` on the endpoint configuration, 30 minutes by default), which discards its remaining chunks as an incomplete message and lets the batch move on (the binary message is then committed with the batch), or until the consumer is stopped or the partition (any partition, with `ProcessAllPartitionsTogether`) is revoked, which rolls the batch back, so that it's consumed again afterwards. Set a max wait time or always read the `Content` to the end.

> [!Note]
> A chunk discarded without being processed (e.g., the remaining chunks of a binary message that the subscriber didn't read to the end, a duplicated chunk or a chunk of an incomplete message) isn't committed while a batch consumed from the same partition (or from any partition, with `ProcessAllPartitionsTogether`) is pending or still being committed, or while the consumer is stopping, since the Kafka commit is cumulative and would implicitly commit the messages of the batch before they're processed. It's committed by the next commit instead. This is usually the case for the remaining chunks of the binary message, which are discarded while the completed batch is being committed, and the chunk being read when the batch completed might be left out of the batch commit as well.
>
> No message is lost and nothing is committed out of order, but until the next message of the partition is committed:
>
> - the consumer lag stays at those few offsets, if the partition is idle;
> - <xref:Silverback.Testing.ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(System.String[])> and <xref:Silverback.Testing.IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(System.String[])> (see <xref:testing>) don't complete and time out, since those offsets are neither stored nor committed;
> - those chunks are consumed and discarded again after a restart or a rebalance.
>
> MQTT isn't affected: the clients with batch processing reject the chunks (see above), and the discarded chunks are acknowledged right away.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producing-chunking> guide
- <xref:default-headers> guide
