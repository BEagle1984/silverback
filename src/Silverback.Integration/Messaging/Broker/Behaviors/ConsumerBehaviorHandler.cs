// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The delegate that describes a message handler in the consumer pipeline.
    /// </summary>
    /// <param name="context">
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services in the current
    ///     pipeline.
    /// </param>
    public delegate Task ConsumerBehaviorHandler(ConsumerPipelineContext context, IServiceProvider serviceProvider);
}
