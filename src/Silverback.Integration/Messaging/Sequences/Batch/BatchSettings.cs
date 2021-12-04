// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Sequences.Batch;

/// <summary>
///     The batch consuming settings.
/// </summary>
public sealed record BatchSettings : IValidatableEndpointSettings
{
    /// <summary>
    ///     Gets the number of messages to be processed in batch. Setting this property to a value
    ///     greater than 1 enables batch consuming.
    /// </summary>
    public int Size { get; init; } = 1;

    /// <summary>
    ///     Gets the maximum amount of time to wait for the batch to be filled. After this time the
    ///     batch will be completed even if the specified <c>Size</c> is not reached.
    /// </summary>
    public TimeSpan? MaxWaitTime { get; init; }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
        if (Size < 1)
            throw new EndpointConfigurationException("Batch.Size must be greater or equal to 1.");

        if (MaxWaitTime != null && MaxWaitTime <= TimeSpan.Zero)
            throw new EndpointConfigurationException("Batch.MaxWaitTime must be greater than 0.");

        if (MaxWaitTime != null && MaxWaitTime.Value.TotalMilliseconds > int.MaxValue)
        {
            throw new EndpointConfigurationException("Sequence.Timeout.TotalMilliseconds must be lower or equal to Int32.MaxValue.");
        }
    }
}
