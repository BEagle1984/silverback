// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     A chain of error policies to be sequentially applied.
    /// </summary>
    public class ErrorPolicyChain : ErrorPolicyBase
    {
        private readonly ILogger<ErrorPolicyChain> _logger;

        private readonly IReadOnlyCollection<ErrorPolicyBase> _policies;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyChain" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        public ErrorPolicyChain(
            IServiceProvider serviceProvider,
            ILogger<ErrorPolicyChain> logger,
            params ErrorPolicyBase[] policies)
            : this(policies.AsEnumerable(), serviceProvider, logger)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyChain" /> class.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public ErrorPolicyChain(
            IEnumerable<ErrorPolicyBase> policies,
            IServiceProvider serviceProvider,
            ILogger<ErrorPolicyChain> logger)
            : base(serviceProvider, logger)
        {
            _logger = logger;

            _policies = Check.NotNull(policies, nameof(policies)).ToList();
            Check.HasNoNulls(_policies, nameof(policies));

            StackMaxFailedAttempts(policies);
        }

        /// <inheritdoc cref="ErrorPolicyBase.ApplyPolicy" />
        protected override Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            foreach (var policy in _policies)
            {
                if (policy.CanHandle(envelopes, exception))
                    return policy.HandleError(envelopes, exception);
            }

            _logger.LogDebug(
                EventIds.ErrorPolicyChainStopConsumer,
                "All policies have been applied but the message(s) couldn't be successfully processed. The consumer will be stopped.",
                envelopes);
            return Task.FromResult(ErrorAction.StopConsuming);
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
