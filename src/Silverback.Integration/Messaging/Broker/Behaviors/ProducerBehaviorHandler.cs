// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     The delegate that describes a message handler in the producer pipeline.
/// </summary>
/// <param name="context">
///     The context that is passed along the producer behaviors pipeline.
/// </param>
/// <returns>
///     A <see cref="ValueTask" /> representing the asynchronous operation.
/// </returns>
public delegate ValueTask ProducerBehaviorHandler(ProducerPipelineContext context);
