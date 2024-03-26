// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWriterFactory" />
public class OutboxWriterFactory : ExtensibleFactory<IOutboxWriter, OutboxSettings>, IOutboxWriterFactory
{
    /// <inheritdoc cref="IOutboxWriterFactory.GetWriter" />
    public IOutboxWriter GetWriter(OutboxSettings settings, IServiceProvider serviceProvider) => GetService(settings, serviceProvider);
}
