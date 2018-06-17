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
        /// <param name="envelope">The envelope containing the message to be handled.</param>
        /// <param name="handler">The method that handles the message.</param>
        public void TryHandleMessage(IEnvelope envelope, Action<IEnvelope> handler)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            try
            {
                handler.Invoke(envelope);
            }
            catch (Exception)
            {
                // TODO: Log & Trace
                HandleError(envelope, handler);
            }
        }

        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <exception cref="Silverback.Messaging.ErrorHandling.ErrorPolicyException"></exception>
        private void HandleError(IEnvelope envelope, Action<IEnvelope> handler)
        {
            try
            {
                ApplyPolicy(envelope, handler);
            }
            catch (Exception e)
            {
                // TODO: Log & Trace (needed?)

                if (_childPolicy != null)
                    _childPolicy.HandleError(envelope, handler);
                else
                    throw new ErrorPolicyException($"Failed to process message '{envelope.Message.Id}'. See InnerException for details.", e);
            }
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        protected abstract void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler);
    }
}