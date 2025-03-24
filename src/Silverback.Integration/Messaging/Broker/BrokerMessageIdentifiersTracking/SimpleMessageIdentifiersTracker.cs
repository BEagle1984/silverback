// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;

/// <summary>
///    Keeps track of the identifiers (offsets or similar) of the messages being produced or consumed.
/// </summary>
public sealed class SimpleMessageIdentifiersTracker : IBrokerMessageIdentifiersTracker
{
    private readonly List<IBrokerMessageIdentifier> _identifiers = [];

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.TrackIdentifier" />
    public void TrackIdentifier(IBrokerMessageIdentifier identifier) => _identifiers.Add(identifier);

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.GetCommitIdentifiers" />
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() => _identifiers;

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.GetRollbackIdentifiers" />
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() => _identifiers;
}
