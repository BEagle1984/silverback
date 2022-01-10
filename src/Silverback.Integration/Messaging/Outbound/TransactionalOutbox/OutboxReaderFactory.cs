// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxReaderFactory" />
public class OutboxReaderFactory : ExtensibleFactory<IOutboxReader, OutboxSettings>, IOutboxReaderFactory
{
    /// <inheritdoc cref="IOutboxReaderFactory.GetReader{TSettings}" />
    public IOutboxReader GetReader<TSettings>(TSettings? settings)
        where TSettings : OutboxSettings =>
        GetService(settings) ??
        throw new InvalidOperationException($"No factory registered for {typeof(TSettings)}. " +
                                            "Please call the necessary Add*Outbox extension method on the BrokerOptionsBuilder.");
}
