// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     In the default configuration this marker interface is used to resolve the types declaring one
    ///     or more message handler method and register them as subscribers.
    /// </summary>
    [SuppressMessage("ReSharper", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface ISubscriber
    {
    }
}
