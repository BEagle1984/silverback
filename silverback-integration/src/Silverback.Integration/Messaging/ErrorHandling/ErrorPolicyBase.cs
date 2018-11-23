using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    public abstract class ErrorPolicyBase : IErrorPolicy
    {
        private readonly ILogger<ErrorPolicyBase> _logger;
        private readonly List<Type> _excludedExceptions = new List<Type>();
        private readonly List<Type> _includedExceptions = new List<Type>();

        protected ErrorPolicyBase(ILogger<ErrorPolicyBase> logger)
        {
            _logger = logger;
        }

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

        protected abstract void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception);

        public ErrorPolicyBase ApplyTo<T>() where T : Exception
        {
            _includedExceptions.Add(typeof(T));
            return this;
        }

        public ErrorPolicyBase Exclude<T>() where T : Exception
        {
            _excludedExceptions.Add(typeof(T));
            return this;
        }
    }
}