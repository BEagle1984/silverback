// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// A chain of error policies to be applied one after another.
    /// </summary>
    public class ErrorPolicyChain : ErrorPolicyBase
    {
        private readonly ILogger<ErrorPolicyChain> _logger;
        private readonly MessageLogger _messageLogger;
        private readonly IEnumerable<ErrorPolicyBase> _policies;

        public ErrorPolicyChain(IPublisher publisher, ILogger<ErrorPolicyChain> logger, MessageLogger messageLogger, params ErrorPolicyBase[] policies)
            : this (policies.AsEnumerable(), publisher, logger, messageLogger)
        {
        }

        public ErrorPolicyChain(IEnumerable<ErrorPolicyBase> policies, IPublisher publisher, ILogger<ErrorPolicyChain> logger, MessageLogger messageLogger)
            : base(publisher, logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;

            _policies = policies ?? throw new ArgumentNullException(nameof(policies));

            if (_policies.Any(p => p == null)) throw new ArgumentNullException(nameof(policies), "One or more policies in the chain have a null value.");
        }

        protected override ErrorAction ApplyPolicy(FailedMessage failedMessage, Exception exception)
        {
            foreach (var policy in _policies)
            {
                if (policy.CanHandle(failedMessage, exception))
                    return policy.HandleError(failedMessage, exception);
            }

            _messageLogger.LogTrace(_logger, "All policies have been applied but the message still couldn't be successfully processed. The consumer will be stopped.", failedMessage);
            return ErrorAction.StopConsuming;
        }
    }
}