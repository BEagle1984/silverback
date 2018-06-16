using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// The base class for all error policies.
    /// </summary>
    public abstract class ErrorPolicyBase
    {
        /// <summary>
        /// Gets or sets the policy to apply after the current one failed.
        /// </summary>
        /// <value>
        /// The child policy.
        /// </value>
        public ErrorPolicyBase ChildPolicy { get; set; }

        /// <summary>
        /// Tries to process the message with the specified handler and takes care of handling
        /// the possible errors.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="handler">The method that handles the message.</param>
        public void TryHandleMessage<T>(T message, Action<T> handler)
            where T : IIntegrationMessage
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            try
            {
                handler.Invoke(message);
            }
            catch (Exception)
            {
                // TODO: Log & Trace
                HandleError(message, handler);
            }

        }

        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <exception cref="Silverback.Messaging.ErrorHandling.ErrorPolicyException"></exception>
        private void HandleError<T>(T message, Action<T> handler)
            where T : IIntegrationMessage
        {
            try
            {
                ApplyPolicy(message, handler);
            }
            catch (Exception e)
            {
                // TODO: Log & Trace (needed?)

                if (ChildPolicy != null)
                    ChildPolicy.HandleError(message, handler);
                else
                    throw new ErrorPolicyException($"Failed to handle error processing message '{message.Id}'. See InnerException for details.", e);
            }
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        protected abstract void ApplyPolicy<T>(T message, Action<T> handler)
            where T : IIntegrationMessage;
    }
}