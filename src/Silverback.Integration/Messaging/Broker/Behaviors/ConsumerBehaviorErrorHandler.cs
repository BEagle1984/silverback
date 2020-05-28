// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The delegate that describes an error (or rollback) handler in the consumer pipeline.
    /// </summary>
    /// <param name="context">
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services in the current
    ///     pipeline.
    /// </param>
    /// <param name="exception">
    ///     The exception that has been thrown during the message processing.
    /// </param>
    public delegate Task ConsumerBehaviorErrorHandler(
        ConsumerPipelineContext context,
        IServiceProvider serviceProvider,
        Exception exception);
}
