// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     This is the default policy and it stops the consumer when an exception is thrown during the
    ///     message processing.
    /// </summary>
    public class StopConsumerErrorPolicy : ErrorPolicyBase
    {
        /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
            new StopConsumerErrorPolicyImplementation(
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider,
                serviceProvider
                    .GetRequiredService<IInboundLogger<StopConsumerErrorPolicy>>());

        private sealed class StopConsumerErrorPolicyImplementation : ErrorPolicyImplementation
        {
            public StopConsumerErrorPolicyImplementation(
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
                IServiceProvider serviceProvider,
                IInboundLogger<StopConsumerErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
            }

            protected override Task<bool> ApplyPolicyAsync(
                ConsumerPipelineContext context,
                Exception exception) =>
                Task.FromResult(false);
        }
    }
}
