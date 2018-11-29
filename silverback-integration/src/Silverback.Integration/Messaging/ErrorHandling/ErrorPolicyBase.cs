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

        public virtual bool CanHandle(IMessage failedMessage, int retryCount, Exception exception)
        {
            if (failedMessage == null)
            {
                _logger.LogTrace($"The policy '{GetType().Name}' cannot be applied because the message is null.");
                return false;
            }

            if (_includedExceptions.Any() && _includedExceptions.All(e => !e.IsInstanceOfType(exception)))
            {
                _logger.LogTrace($"The policy '{GetType().Name}' will be skipped because the {exception.GetType().Name} " +
                                 $"is not in the list of handled exceptions.");

                return false;
            }

            if (_excludedExceptions.Any(e => e.IsInstanceOfType(exception)))
            {
                _logger.LogTrace($"The policy '{GetType().Name}' will be skipped because the {exception.GetType().Name} " +
                                 $"is in the list of excluded exceptions.");

                return false;
            }

            return true;
        }

        public abstract ErrorAction HandleError(IMessage failedMessage, int retryCount, Exception exception);
    }
}