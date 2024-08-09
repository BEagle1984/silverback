// Copyright (c) 2024 Sergio Aquilini
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
    public static void SetKafkaOffsetStoreScope(this ISilverbackContext context, KafkaOffsetStoreScope scope) =>
        Check.NotNull(context, nameof(context)).AddObject(OffsetStoreObjectTypeId, scope);

    /// <summary>
    ///     Returns the <see cref="KafkaOffsetStoreScope" /> to be used to store the offsets.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public static KafkaOffsetStoreScope GetKafkaOffsetStoreScope(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).GetObject<KafkaOffsetStoreScope>(OffsetStoreObjectTypeId);
}
