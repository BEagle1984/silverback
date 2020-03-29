// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     A chain of error policies to be applied one after another.
    /// </summary>
    public class ErrorPolicyChain : ErrorPolicyBase
    {
        private readonly ILogger<ErrorPolicyChain> _logger;
        private readonly MessageLogger _messageLogger;
        private readonly IEnumerable<ErrorPolicyBase> _policies;

        public ErrorPolicyChain(
            IServiceProvider serviceProvider,
            ILogger<ErrorPolicyChain> logger,
            MessageLogger messageLogger,
            params ErrorPolicyBase[] policies)
            : this(policies.AsEnumerable(), serviceProvider, logger, messageLogger)
        {
        }

        public ErrorPolicyChain(
            IEnumerable<ErrorPolicyBase> policies,
            IServiceProvider serviceProvider,
            ILogger<ErrorPolicyChain> logger,
            MessageLogger messageLogger)
            : base(serviceProvider, logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;

            _policies = policies ?? throw new ArgumentNullException(nameof(policies));

            StackMaxFailedAttempts(policies);

            if (_policies.Any(p => p == null))
                throw new ArgumentNullException(nameof(policies),
                    "One or more policies in the chain have a null value.");
        }

        protected override ErrorAction ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            foreach (var policy in _policies)
            {
                if (policy.CanHandle(envelopes, exception))
                    return policy.HandleError(envelopes, exception);
            }

            _messageLogger.LogDebug(_logger,
                "All policies have been applied but the message(s) couldn't be successfully processed. The consumer will be stopped.",
                envelopes);
            return ErrorAction.StopConsuming;
        }

        private static void StackMaxFailedAttempts(IEnumerable<ErrorPolicyBase> policies)
        {
            var totalAttempts = 0;
            foreach (var policy in policies)
            {
                if (policy.MaxFailedAttemptsSetting <= 0)
                    continue;

                totalAttempts += policy.MaxFailedAttemptsSetting;
                policy.MaxFailedAttempts(totalAttempts);
            }
        }
    }
}