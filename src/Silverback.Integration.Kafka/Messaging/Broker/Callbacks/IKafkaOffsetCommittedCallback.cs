// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnOffsetsCommitted" /> event handler.
/// </summary>
public interface IKafkaOffsetCommittedCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called to report the result of offset commits.
    /// </summary>
    /// <remarks>
    ///     Possible error conditions:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Entire request failed: <c>Error</c> is set, but not per-partition errors.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 All partitions failed: <c>Error</c> is set to the value of the last failed partition, but
    ///                 each partition may have different errors.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Some partitions failed: global <c>Error</c> is success
    ///                 (<see cref="Confluent.Kafka.ErrorCode.NoError" />).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="offsets">
    ///     The per-partition offsets and success or error information and the overall operation success or error information.
    /// </param>
    /// <param name="consumer">
    ///     The related consumer instance.
    /// </param>
    void OnOffsetsCommitted(CommittedOffsets offsets, KafkaConsumer consumer);
}
