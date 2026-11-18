// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Provides enrichment for the produce and consume activities.
/// </summary>
public interface IBrokerActivityEnricher
{
    /// <summary>
    ///     Enriches the produce activity right before the produced message is logged.
    /// </summary>
    /// <param name="activity">
    ///     The <see cref="Activity" /> to be enriched.
    /// </param>
    /// <param name="envelope">
    ///     The envelope of the produced message.
    /// </param>
    void EnrichOutboundActivity(Activity activity, IOutboundEnvelope envelope);

    /// <summary>
    ///     Enriches the consume activity when the message has been received, before it is processed.
    /// </summary>
    /// <param name="activity">
    ///     The <see cref="Activity" /> to be enriched.
    /// </param>
    /// <param name="envelope">
    ///     The envelope of the consumed message, which hasn't been deserialized yet.
    /// </param>
    void EnrichInboundActivity(Activity activity, IRawInboundEnvelope envelope);
}
