// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxReaderFactory" />
public class OutboxReaderFactory : ExtensibleFactory<IOutboxReader, OutboxSettings>, IOutboxReaderFactory
{
    /// <inheritdoc cref="IOutboxReaderFactory.GetReader" />
    public IOutboxReader GetReader(OutboxSettings settings) => GetService(Check.NotNull(settings, nameof(settings)));
}
