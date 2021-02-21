// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Broker.Callbacks
{
    /// <summary>
    ///     The marker interface implemented by all interfaces declaring the broker callbacks handlers.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface IBrokerCallback
    {
    }
}
