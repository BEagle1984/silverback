// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     The delegate that describes a message handler in the consumer pipeline.
/// </summary>
/// <param name="context">
///     The context that is passed along the consumer behaviors pipeline.
/// </param>
/// <param name="cancellationToken">
///     The cancellation token that can be used to cancel the operation.
/// </param>
/// <returns>
///     A <see cref="ValueTask" /> representing the asynchronous operation.
/// </returns>
public delegate ValueTask ConsumerBehaviorHandler(ConsumerPipelineContext context, CancellationToken cancellationToken);
