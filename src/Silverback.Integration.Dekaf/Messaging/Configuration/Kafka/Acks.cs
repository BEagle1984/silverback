// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The number of acknowledgements that the leader broker must receive from the in-sync replicas before
///     responding to the produce request.
/// </summary>
public enum Acks
{
    /// <summary>
    ///     The broker doesn't send any response or acknowledgement.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The leader writes the record to its local log and responds without waiting for the followers.
    /// </summary>
    Leader = 1,

    /// <summary>
    ///     The leader waits for the record to be committed by all in-sync replicas.
    /// </summary>
    All = -1
}
