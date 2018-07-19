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
        private ILogger _logger;

        /// <summary>
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public virtual void Init(IBus bus)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<ErrorPolicyBase>();
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
                                       $"The policy '{this}' will be applied.");
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
                _logger.LogWarning(ex, $"The policy was applied but the message " +
                                       $"'{envelope.Message.Id}' still couldn't be successfully " +
                                       $"processed. An exception will be thrown.");
                throw new ErrorPolicyException($"Failed to process message '{envelope.Message.Id}'. See InnerException for details.", ex);
            }
        }

        /// <summary>
        /// When implemented in a derived class applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        public abstract void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler);
    }
}