// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Contains the sort index constants of the default <see cref="IBehavior" /> added by
///     Silverback.Integration.
/// </summary>
public static class IntegrationBehaviorsSortIndexes
{
    /// <summary>
    ///     The <see cref="ProduceBehavior" /> sort index.
    /// </summary>
    public const int OutboundProducer = 200;

    /// <summary>
    ///     The <see cref="OutboundRouterBehavior" /> sort index.
    /// </summary>
    public const int OutboundRouter = 300;
}
