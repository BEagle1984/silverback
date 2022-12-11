// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Contains the constants with the names of the Kafka specific message headers used by Silverback.
/// </summary>
public static class KafkaMessageHeaders
{
    /// <summary>
    ///     The header that will be filled with the key of the message consumed from Kafka. It is also used to
    ///     temporary store the key for the produced message, defined via <see cref="KafkaKeyMemberAttribute" />.
    /// </summary>
    public const string KafkaMessageKey = "x-kafka-message-key";

    /// <summary>
    ///     The header that will be filled with the timestamp of the message consumed from Kafka.
    /// </summary>
    public const string Timestamp = "x-kafka-message-timestamp";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the GroupId of
    ///     the consumer that consumed the message that failed to be processed.
    /// </summary>
    public const string SourceConsumerGroupId = "x-source-consumer-group-id";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the source
    ///     topic of the message that failed to be processed.
    /// </summary>
    public const string SourceTopic = "x-source-topic";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the source
    ///     partition of the message that failed to be processed.
    /// </summary>
    public const string SourcePartition = "x-source-partition";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the offset of
    ///     the message that failed to be processed.
    /// </summary>
    public const string SourceOffset = "x-source-offset";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the timestamp of the message that failed to be
    ///     processed.
    /// </summary>
    public const string SourceTimestamp = "x-source-timestamp";

    /// <summary>
    ///     The <see cref="KafkaOffset" /> value of the first chunk of the same message, set when producing a chunked message.
    /// </summary>
    public const string FirstChunkOffset = "x-chunk-first-offset";
}
