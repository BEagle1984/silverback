// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Publishing;

/// <summary>
///     The interface that must be implemented by all publishers to allow for extensions.
/// </summary>
public interface IPublisherBase
{
    /// <summary>
    ///     Gets the <see cref="SilverbackContext" /> in the current scope.
    /// </summary>
    public SilverbackContext Context { get; }
}
