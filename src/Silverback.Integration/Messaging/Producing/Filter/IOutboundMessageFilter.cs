// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Filter;

/// <summary>
///     Can be used to filter out messages that should not be produced.
/// </summary>
public interface IOutboundMessageFilter
{
    /// <summary>
    ///     Returns a value indicating whether the specified message should be produced.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be filtered.
    /// </param>
    /// <returns>
    ///     A value indicating whether the message should be produced.
    /// </returns>
    bool ShouldProduce(IOutboundEnvelope envelope);
}
