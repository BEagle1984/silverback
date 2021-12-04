// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     This <see cref="ISequenceStoreCollection" /> uses a single <see cref="ISequenceStore" />.
/// </summary>
internal sealed class DefaultSequenceStoreCollection : ISequenceStoreCollection
{
    private readonly object _lockObject = new();

    private readonly IServiceProvider _serviceProvider;

    private ISequenceStore _sequenceStore;

    private bool _disposed;

    public DefaultSequenceStoreCollection(IServiceProvider serviceProvider)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _sequenceStore = _serviceProvider.GetRequiredService<ISequenceStore>();
    }

    public int Count => 1;

    public ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        if (!_sequenceStore.Disposed)
            return _sequenceStore;

        lock (_lockObject)
        {
            if (_sequenceStore.Disposed && !_disposed)
                _sequenceStore = _serviceProvider.GetRequiredService<ISequenceStore>();
        }

        return _sequenceStore;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public IEnumerator<ISequenceStore> GetEnumerator()
    {
        yield return _sequenceStore;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _sequenceStore.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
