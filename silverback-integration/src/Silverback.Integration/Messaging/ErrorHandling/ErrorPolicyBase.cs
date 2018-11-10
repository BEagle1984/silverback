using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<Type> _excludedExceptions = new List<Type>();
        private readonly List<Type> _includedExceptions = new List<Type>();

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

                if (!ApplyPolicy(envelope, handler, ex))
                    throw;
            }
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public bool ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
        {
            if (!MustHandle(exception))
                return false;

            try
            {
                ApplyPolicyImpl(envelope, handler, exception);

                return true;
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
        /// Returns a boolean value indicating whether this policy must be applied to the specified exception according to
        /// included/excluded exception types.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        protected bool MustHandle(Exception exception)
        {
            if (_includedExceptions.Any() && _includedExceptions.All(e => !e.IsInstanceOfType(exception)))
            {
                _logger.LogTrace($"The policy '{this}' will be skipped because the {exception.GetType().Name} " +
                                 $"is not in the list of handled exceptions.");

                return false;
            }

            if (_excludedExceptions.Any(e => e.IsInstanceOfType(exception)))
            {
                _logger.LogTrace($"The policy '{this}' will be skipped because the {exception.GetType().Name} " +
                                 $"is in the list of excluded exceptions.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// When implemented in a derived class applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <param name="exception">The exception that occurred.</param>
        protected abstract void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception);

        /// <summary>
        /// Applies this policy only to the exceptions of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public ErrorPolicyBase ApplyTo<T>() where T : Exception
        {
            _includedExceptions.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Bypass this policy for exceptions of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public ErrorPolicyBase Exclude<T>() where T : Exception
        {
            _excludedExceptions.Add(typeof(T));
            return this;
        }
    }
}