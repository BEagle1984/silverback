// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Contains the constants with the names of the standard message headers used by Silverback.
/// </summary>
public static class DefaultMessageHeaders
{
    /// <summary>
    ///     The message identifier.
    /// </summary>
    /// <remarks>
    ///     This header is used as Kafka key when producing the message and filled with the value of the Kafka key when consuming.
    /// </remarks>
    public const string MessageId = "x-message-id";

    /// <summary>
    ///     The assembly qualified name of the message type. Used by the default <see cref="JsonMessageSerializer" />.
    /// </summary>
    public const string MessageType = "x-message-type";

    /// <summary>
    ///     If an exception if thrown the failed attempts will be incremented and stored as header. This is necessary for the error policies
    ///     to work.
    /// </summary>
    public const string FailedAttempts = "x-failed-attempts";

    /// <summary>
    ///     The message chunk index, used when chunking is enabled.
    /// </summary>
    public const string ChunkIndex = "x-chunk-index";

    /// <summary>
    ///     The total number of chunks the message was split into, used when chunking is enabled.
    /// </summary>
    public const string ChunksCount = "x-chunk-count";

    /// <summary>
    ///     A boolean value indicating whether the message is the last one of a chunks sequence, used when
    ///     chunking is enabled.
    /// </summary>
    public const string IsLastChunk = "x-chunk-last";

    /// <summary>
    ///     Used for distributed tracing. It is set by the <see cref="IProducer" /> using the current
    ///     <c>Activity.Id</c>. The <see cref="IConsumer" /> uses it's value to set the
    ///     <c>Activity.ParentId</c>. Note that an <c>Activity</c> is automatically started by the default
    ///     <see cref="IProducer" /> implementation.
    /// </summary>
    /// <remarks>
    ///     The header is implemented according to the W3C Trace Context proposal
    ///     (https://www.w3.org/TR/trace-context-1/#traceparent-header).
    /// </remarks>
    public const string TraceId = "traceparent";

    /// <summary>
    ///     Used for distributed tracing. It corresponds to the <c>Activity.TraceStateString</c>.
    /// </summary>
    /// <remarks>
    ///     The header is implemented according to the W3C Trace Context proposal
    ///     (https://www.w3.org/TR/trace-context-1/#tracestate-header).
    /// </remarks>
    public const string TraceState = "tracestate";

    /// <summary>
    ///     Used for distributed tracing. It corresponds to the string representation of the
    ///     <c>Activity.Baggage</c> dictionary.
    /// </summary>
    /// <remarks>
    ///     This is not part of the w3c standard.
    /// </remarks>
    public const string TraceBaggage = "tracebaggage";

    /// <summary>
    ///     The MIME type of a binary message. See <see cref="IBinaryMessage" />.
    /// </summary>
    public const string ContentType = "content-type";

    /// <summary>
    ///     The encryption key identifier.
    /// </summary>
    /// <remarks>
    ///     The header is required for the key rotation feature. When rotating keys, it will be used on the
    ///     consumer side to determine the correct key to be used to decrypt the message.
    /// </remarks>
    public const string EncryptionKeyId = "x-encryption-key-id";

    /// <summary>
    ///     This will be set by the <see cref="MoveMessageErrorPolicy" /> and will contain the reason why
    ///     the message failed to be processed.
    /// </summary>
    public const string FailureReason = "x-failure-reason";
}
