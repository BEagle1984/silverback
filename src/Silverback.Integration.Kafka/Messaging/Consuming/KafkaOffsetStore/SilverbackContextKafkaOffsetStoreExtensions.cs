// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Adds the Kafka offset store specific methods to the <see cref="ISilverbackContext" />.
/// </summary>
public static class SilverbackContextKafkaOffsetStoreExtensions
{
    private static readonly Guid OffsetStoreObjectTypeId = new("9d9795c6-4b91-43ee-b370-fa0f539a20f8");

    /// <summary>
    ///     Sets the <see cref="KafkaOffsetStoreScope" /> to be used to store the offsets.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <param name="scope">
    ///     The scope.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     A different scope is already set in the context.
    /// </exception>
    public static void SetKafkaOffsetStoreScope(this ISilverbackContext context, KafkaOffsetStoreScope scope) =>
        Check.NotNull(context).AddObject(OffsetStoreObjectTypeId, scope);

    /// <summary>
    ///     Returns the <see cref="KafkaOffsetStoreScope" /> to be used to store the offsets.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOffsetStoreScope" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No <see cref="KafkaOffsetStoreScope" /> is set in the context, because the consumer doesn't use a client-side offset store or
    ///     the call isn't made while consuming.
    /// </exception>
    public static KafkaOffsetStoreScope GetKafkaOffsetStoreScope(this ISilverbackContext context) =>
        Check.NotNull(context).GetObject<KafkaOffsetStoreScope>(OffsetStoreObjectTypeId);
}
