// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     Can be used to build a custom pipeline, plugging some functionality into either the
///     <see cref="IProducer" /> (see <see cref="IProducerBehavior" />) or the <see cref="IConsumer" /> (see
///     <see cref="IConsumerBehavior" />).
/// </summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface")]
public interface IBrokerBehavior
{
}
