// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     The chunking settings. To enable chunking just set the <c>Size</c> property to the desired (maximum)
///     chunk size.
/// </summary>
public sealed record ChunkSettings : IValidatableSettings
{
    /// <summary>
    ///     Gets the size in bytes of each chunk. The default is <see cref="int.MaxValue" />, meaning that
    ///     chunking is disabled.
    /// </summary>
    public int Size { get; init; } = int.MaxValue;

    /// <summary>
    ///     Gets a value indicating whether the <c>x-chunk-index</c> and related headers have to be added
    ///     to the produced message in any case, even if its size doesn't exceed the single chunk size. The
    ///     default is <c>true</c>. This setting is ignored if chunking is disabled (<see cref="Size" /> is not
    ///     set).
    /// </summary>
    public bool AlwaysAddHeaders { get; init; } = true;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (Size < 1)
            throw new BrokerConfigurationException("The chunk size must be greater or equal to 1.");
    }
}
