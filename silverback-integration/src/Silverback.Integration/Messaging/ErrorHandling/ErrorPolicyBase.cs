// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
        private Func<FailedMessage, Exception, bool> _applyRule;
        private int _maxFailedAttempts = -1;

        protected ErrorPolicyBase(ILogger<ErrorPolicyBase> logger)
        {
            _logger = logger;
        }

        public ErrorPolicyBase ApplyTo<T>() where T : Exception
        {
            ApplyTo(typeof(T));
            return this;
        }

        public ErrorPolicyBase ApplyTo(Type exceptionType)
        {
            _includedExceptions.Add(exceptionType);
            return this;
        }

        public ErrorPolicyBase Exclude<T>() where T : Exception
        {
            Exclude(typeof(T));
            return this;
        }

        public ErrorPolicyBase Exclude(Type exceptionType)
        {
            _excludedExceptions.Add(exceptionType);
            return this;
        }

        public ErrorPolicyBase ApplyWhen(Func<FailedMessage, Exception, bool> applyRule)
        {
            _applyRule = applyRule;
            return this;
        }

        public ErrorPolicyBase MaxFailedAttempts(int maxFailedAttempts)
        {
            _maxFailedAttempts = maxFailedAttempts;
            return this;
        }

        public virtual bool CanHandle(FailedMessage failedMessage, Exception exception)
        {
            if (failedMessage == null)
            {
                _logger.LogTrace($"The policy '{GetType().Name}' cannot be applied because the message is null.");
                return false;
            }

            if (_maxFailedAttempts >= 0 && failedMessage.FailedAttempts > _maxFailedAttempts)
            {
                _logger.LogMessageTrace($"The policy '{GetType().Name}' will be skipped because the current failed attempts " +
                                 $"({failedMessage.FailedAttempts}) exceeds the configured maximum attempts " +
                                 $"({_maxFailedAttempts}).", failedMessage);

                return false;
            }

            if (_includedExceptions.Any() && _includedExceptions.All(e => !e.IsInstanceOfType(exception)))
            {
                _logger.LogMessageTrace($"The policy '{GetType().Name}' will be skipped because the {exception.GetType().Name} " +
                                 $"is not in the list of handled exceptions.", failedMessage);

                return false;
            }

            if (_excludedExceptions.Any(e => e.IsInstanceOfType(exception)))
            {
                _logger.LogMessageTrace($"The policy '{GetType().Name}' will be skipped because the {exception.GetType().Name} " +
                                 $"is in the list of excluded exceptions.", failedMessage);

                return false;
            }

            if (_applyRule != null && !_applyRule.Invoke(failedMessage, exception))
            {
                _logger.LogMessageTrace($"The policy '{GetType().Name}' will be skipped because the apply rule has been " +
                                 $"evaluated and returned false.", failedMessage);
                return false;
            }

            return true;
        }

        public abstract ErrorAction HandleError(FailedMessage failedMessage, Exception exception);
    }
}