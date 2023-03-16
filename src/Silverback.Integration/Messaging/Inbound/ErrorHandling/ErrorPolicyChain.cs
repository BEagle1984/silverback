// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     A chain of error policies to be sequentially applied.
    /// </summary>
    public class ErrorPolicyChain : IErrorPolicy
    {
        private bool _failedAttemptsStacked;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyChain" /> class.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        public ErrorPolicyChain(params ErrorPolicyBase[] policies)
            : this(policies.AsEnumerable())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyChain" /> class.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        public ErrorPolicyChain(IEnumerable<ErrorPolicyBase> policies)
        {
            Policies = Check.NotNull(policies, nameof(policies)).ToList();
            Check.HasNoNulls(Policies, nameof(policies));
        }

        internal IReadOnlyCollection<ErrorPolicyBase> Policies { get; }

        /// <inheritdoc cref="IErrorPolicy.Build" />
        public IErrorPolicyImplementation Build(IServiceProvider serviceProvider) =>
            new ErrorPolicyChainImplementation(
                StackMaxFailedAttempts(Policies)
                    .Select(policy => policy.Build(serviceProvider))
                    .Cast<ErrorPolicyImplementation>(),
                serviceProvider.GetRequiredService<IInboundLogger<ErrorPolicyChainImplementation>>());

        private IReadOnlyCollection<ErrorPolicyBase> StackMaxFailedAttempts(IReadOnlyCollection<ErrorPolicyBase> policies)
        {
            if (_failedAttemptsStacked)
                return policies;

            var totalAttempts = 0;
            foreach (var policy in policies)
            {
                if (policy.MaxFailedAttemptsCount is null or <= 0)
                    continue;

                totalAttempts += policy.MaxFailedAttemptsCount.Value;
                policy.MaxFailedAttemptsCount = totalAttempts;
            }

            _failedAttemptsStacked = true;

            return policies;
        }

        private sealed class ErrorPolicyChainImplementation : IErrorPolicyImplementation
        {
            private readonly IInboundLogger<ErrorPolicyChainImplementation> _logger;

            private readonly IReadOnlyCollection<ErrorPolicyImplementation> _policies;

            public ErrorPolicyChainImplementation(
                IEnumerable<ErrorPolicyImplementation> policies,
                IInboundLogger<ErrorPolicyChainImplementation> logger)
            {
                _policies = Check.NotNull(policies, nameof(policies)).ToList();
                Check.HasNoNulls(_policies, nameof(policies));

                _logger = Check.NotNull(logger, nameof(logger));
            }

            public bool CanHandle(ConsumerPipelineContext context, Exception exception) => true;

            public Task<bool> HandleErrorAsync(ConsumerPipelineContext context, Exception exception)
            {
                Check.NotNull(context, nameof(context));
                Check.NotNull(exception, nameof(exception));

                var nextPolicy = _policies.FirstOrDefault(policy => policy.CanHandle(context, exception));

                if (nextPolicy != null)
                    return nextPolicy.HandleErrorAsync(context, exception);

                _logger.LogInboundTrace(IntegrationLogEvents.PolicyChainCompleted, context.Envelope);

                return Task.FromResult(false);
            }
        }
    }
}
