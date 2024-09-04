// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.Transaction;

/// <summary>
///     Adds the <see cref="GetConsumerPipelineContext" /> method to the <see cref="ISilverbackContext" />.
/// </summary>
public static class SilverbackContextConsumerPipelineContextExtensions
{
    private static readonly Guid ConsumerPipelineContextObjectTypeId = new("225af6cd-61a2-489c-b7e1-d177c2e1a575");

    /// <summary>
    ///     Gets the current <see cref="ConsumerPipelineContext" />.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="ConsumerPipelineContext" />.
    /// </returns>
    public static ConsumerPipelineContext GetConsumerPipelineContext(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).GetObject<ConsumerPipelineContext>(ConsumerPipelineContextObjectTypeId);

    /// <summary>
    ///     Checks whether a <see cref="ConsumerPipelineContext" /> is set and returns it.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <param name="pipelineContext">
    ///     The <see cref="ConsumerPipelineContext" />.
    /// </param>
    /// <returns>
    ///     A value indicating whether the <see cref="ConsumerPipelineContext" /> was found.
    /// </returns>
    public static bool TryGetConsumerPipelineContext(this ISilverbackContext context, out ConsumerPipelineContext? pipelineContext) =>
        Check.NotNull(context, nameof(context)).TryGetObject(ConsumerPipelineContextObjectTypeId, out pipelineContext);

    internal static void SetConsumerPipelineContext(this ISilverbackContext context, ConsumerPipelineContext pipelineContext) =>
        Check.NotNull(context, nameof(context)).SetObject(ConsumerPipelineContextObjectTypeId, pipelineContext);
}
