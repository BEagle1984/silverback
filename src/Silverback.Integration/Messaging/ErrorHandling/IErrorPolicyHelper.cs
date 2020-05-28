// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     Exposes a method used to wrap the message handler delegate invocation and apply the provided
    ///     <see cref="IErrorPolicy" />.
    /// </summary>
    public interface IErrorPolicyHelper
    {
        /// <summary>
        ///     Wraps the message handler delegate invocation and takes care of the error handling applying the
        ///     provided <see cref="IErrorPolicy" />.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the behaviors pipeline.
        /// </param>
        /// <param name="errorPolicy">
        ///     The error policy to be applied. If no policy is provided the consumer will be stopped whenever an
        ///     exception is thrown by the message handler delegete.
        /// </param>
        /// <param name="messagesHandler">
        ///     The delegate to be invoked to process the message.
        /// </param>
        /// <param name="commitHandler">
        ///     The delegate to be invoked to commit the message processing (against the database and the message
        ///     broker).
        /// </param>
        /// <param name="rollbackHandler">
        ///     The delegate to be invoked to rollback the pending operations (against the database and the message
        ///     broker) in case of exception.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task TryProcessAsync(
            ConsumerPipelineContext context,
            IErrorPolicy? errorPolicy,
            ConsumerBehaviorHandler messagesHandler,
            ConsumerBehaviorHandler commitHandler,
            ConsumerBehaviorErrorHandler rollbackHandler);
    }
}
