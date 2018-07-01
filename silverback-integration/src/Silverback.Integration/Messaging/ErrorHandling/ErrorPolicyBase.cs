using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// The base class for all error policies.
    /// </summary>
    public abstract class ErrorPolicyBase : IErrorPolicy
    {
        private ErrorPolicyBase _childPolicy;
        private ILogger _logger;

        /// <summary>
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public virtual void Init(IBus bus)
        {
            _childPolicy?.Init(bus);
            _logger = bus.GetLoggerFactory().CreateLogger<ErrorPolicyBase>();
        }

        /// <summary>
        /// Wraps the specified policy.
        /// </summary>
        /// <param name="policy">The policy to be executed if this one fails.</param>
        public virtual IErrorPolicy Wrap(IErrorPolicy policy)
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"An error occurred handling the message '{envelope.Message.Id}'. " +
                                       $"The policy '{GetType().Name}' will be applied.");
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
            catch (Exception ex)
            {
                if (_childPolicy != null)
                {
                    _logger.LogInformation(ex, $"The error policy has been applied but the message " +
                                               $"'{envelope.Message.Id}' still couldn't be successfully " +
                                               $"processed. Will continue applying the next policy " +
                                               $"({_childPolicy.GetType().Name}");

                    _childPolicy.HandleError(envelope, handler);
                }
                else
                {
                    _logger.LogWarning(ex, $"All policies have been applied but the message " +
                                           $"'{envelope.Message.Id}' still couldn't be successfully " +
                                           $"processed. An exception will be thrown.");
                    throw new ErrorPolicyException($"Failed to process message '{envelope.Message.Id}'. See InnerException for details.", ex);
                }
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