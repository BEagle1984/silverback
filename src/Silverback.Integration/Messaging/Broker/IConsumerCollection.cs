// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Holds a reference to all the configured <see cref="IConsumer" />.
/// </summary>
public interface IConsumerCollection : IReadOnlyCollection<IConsumer>
{
    /// <summary>
    ///     Gets the consumer with the specified name.
    /// </summary>
    /// <param name="name">
    ///     The consumer name.
    /// </param>
    /// <returns>
    ///     The <see cref="IConsumer" /> with the specified name.
    /// </returns>
    IConsumer this[string name] { get; }
}
