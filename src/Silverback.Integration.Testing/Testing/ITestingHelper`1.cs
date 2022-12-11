// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Testing;

/// <summary>
///     Exposes some helper methods and shortcuts to simplify testing.
/// </summary>
public partial interface ITestingHelper
{
    /// <summary>
    ///     Gets the <see cref="IIntegrationSpy" />.
    /// </summary>
    /// <remarks>
    ///     The <see cref="IIntegrationSpy" /> must be enabled calling <c>AddIntegrationSpy</c> or
    ///     <c>AddIntegrationSpyAndSubscriber</c>.
    /// </remarks>
    IIntegrationSpy Spy { get; }
}
