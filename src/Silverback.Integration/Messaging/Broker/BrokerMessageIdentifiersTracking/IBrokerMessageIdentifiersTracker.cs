// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;

/// <summary>
///     Keeps track of the identifiers (offsets or similar) of the messages being produced or consumed.
/// </summary>
public interface IBrokerMessageIdentifiersTracker
{
    /// <summary>
    ///     Tracks the specified identifier.
    /// </summary>
    /// <param name="identifier">
    ///    The identifier to be tracked.
    /// </param>
    void TrackIdentifier(IBrokerMessageIdentifier identifier);

    /// <summary>
    ///     Gets the identifiers to be used to commit after successful processing.
    /// </summary>
    /// <returns>
    ///     The identifiers to be used to commit.
    /// </returns>
    IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers();

    /// <summary>
    ///     Gets the identifiers to be used to rollback in case of error.
    /// </summary>
    /// <returns>
    ///     The identifiers to be used to rollback.
    /// </returns>
    IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers();
}
