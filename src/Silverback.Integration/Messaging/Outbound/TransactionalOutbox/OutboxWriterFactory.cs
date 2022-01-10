// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWriterFactory" />
public class OutboxWriterFactory : ExtensibleFactory<IOutboxWriter, OutboxSettings>, IOutboxWriterFactory
{
    /// <inheritdoc cref="IOutboxWriterFactory.GetWriter{TSettings}" />
    public IOutboxWriter GetWriter<TSettings>(TSettings? settings)
        where TSettings : OutboxSettings =>
        GetService(settings) ??
        throw new InvalidOperationException(
            $"No factory registered for {typeof(TSettings)}. " +
            "Please call the necessary Add*Outbox extension method on the BrokerOptionsBuilder.");
}
