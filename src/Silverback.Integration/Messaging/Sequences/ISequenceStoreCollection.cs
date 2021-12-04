// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     The collection of <see cref="ISequenceStore" /> used by the <see cref="IConsumer" />.
/// </summary>
public interface ISequenceStoreCollection : IReadOnlyCollection<ISequenceStore>, IAsyncDisposable
{
    /// <summary>
    ///     Returns the <see cref="ISequenceStore" /> to be used to store the pending sequences.
    /// </summary>
    /// <param name="brokerMessageIdentifier">
    ///     The message identifier (the offset in Kafka) may determine which store is being used. For example a
    ///     dedicated sequence store is used per each Kafka partition, since they may be processed concurrently.
    /// </param>
    /// <returns>
    ///     The <see cref="ISequenceStore" />.
    /// </returns>
    ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier);
}
