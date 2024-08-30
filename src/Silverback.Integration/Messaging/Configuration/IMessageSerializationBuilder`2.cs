// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Exposes the <see cref="SerializeUsing" /> method, allowing to specify the <see cref="IMessageSerializer" /> to be used to serialize the messages.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public interface IMessageSerializationBuilder<TMessage, TBuilder>
{
    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the messages.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    TBuilder SerializeUsing(IMessageSerializer serializer);
}
