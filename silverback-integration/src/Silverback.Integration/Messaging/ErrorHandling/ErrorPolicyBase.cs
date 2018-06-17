using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// The base class for all error policies.
    /// </summary>
    public abstract class ErrorPolicyBase : IErrorPolicy
    {
        private ErrorPolicyBase _childPolicy;

        /// <summary>
        /// Wraps the specified policy.
        /// </summary>
        /// <param name="policy">The policy to be executed if this one fails.</param>
        /// <exception cref="ArgumentNullException">policy</exception>
        /// <exception cref="ArgumentException">The wrapped policy must inherit from ErrorPolicyBase. - policy</exception>
        public IErrorPolicy Wrap(IErrorPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            _childPolicy = policy as ErrorPolicyBase;

            if (_childPolicy == null) throw new ArgumentException("The wrapped policy must inherit from ErrorPolicyBase.", nameof(policy));

            return this;
        }

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

                if (_childPolicy != null)
                    _childPolicy.HandleError(message, handler);
                else
                    throw new ErrorPolicyException($"Failed to process message '{message.Id}'. See InnerException for details.", e);
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