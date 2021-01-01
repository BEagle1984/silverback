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
        private readonly IReadOnlyCollection<ErrorPolicyBase> _policies;

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
            _policies = Check.NotNull(policies, nameof(policies)).ToList();
            Check.HasNoNulls(_policies, nameof(policies));
        }

        /// <inheritdoc cref="IErrorPolicy.Build" />
        public IErrorPolicyImplementation Build(IServiceProvider serviceProvider) =>
            new ErrorPolicyChainImplementation(
                StackMaxFailedAttempts(_policies)
                    .Select(policy => policy.Build(serviceProvider))
                    .Cast<ErrorPolicyImplementation>(),
                serviceProvider.GetRequiredService<IInboundLogger<ErrorPolicyChainImplementation>>());

        private static IReadOnlyCollection<ErrorPolicyBase> StackMaxFailedAttempts(
            IReadOnlyCollection<ErrorPolicyBase> policies)
        {
            var totalAttempts = 0;
            foreach (var policy in policies)
            {
                if (policy.MaxFailedAttemptsCount == null || policy.MaxFailedAttemptsCount <= 0)
                    continue;

                totalAttempts += policy.MaxFailedAttemptsCount.Value;
                policy.MaxFailedAttemptsCount = totalAttempts;
            }

            return policies;
        }

        private class ErrorPolicyChainImplementation : IErrorPolicyImplementation
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

                foreach (var policy in _policies)
                {
                    if (policy.CanHandle(context, exception))
                        return policy.HandleErrorAsync(context, exception);
                }

                _logger.LogInboundTrace(IntegrationLogEvents.PolicyChainCompleted, context.Envelope);

                return Task.FromResult(false);
            }
        }
    }
}
