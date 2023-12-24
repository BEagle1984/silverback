// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Represents a Kafka transaction.
/// </summary>
public interface IKafkaTransaction : IDisposable
{
    /// <summary>
    ///     Gets the suffix to be appended to the configured transactional id.
    /// </summary>
    /// <remarks>
    ///     This is used to enable multiple transactions per producer configuration.
    /// </remarks>
    string? TransactionalIdSuffix { get; }

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    public void Commit();

    /// <summary>
    ///     Aborts the transaction.
    /// </summary>
    public void Abort();
}
