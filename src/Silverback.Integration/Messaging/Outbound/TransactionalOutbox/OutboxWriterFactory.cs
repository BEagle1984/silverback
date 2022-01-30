// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWriterFactory" />
public class OutboxWriterFactory : ExtensibleFactory<IOutboxWriter, OutboxSettings>, IOutboxWriterFactory
{
    /// <inheritdoc cref="IOutboxWriterFactory.GetWriter" />
    public IOutboxWriter GetWriter(OutboxSettings settings) => GetService(Check.NotNull(settings, nameof(settings)));
}
