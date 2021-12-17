// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    internal static class ErrorPoliciesHelper
    {
        public static async Task<bool> ApplyErrorPoliciesAsync(
            ConsumerPipelineContext context,
            Exception exception)
        {
            var failedAttempts = context.Consumer.IncrementFailedAttempts(context.Envelope);

            context.Envelope.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, failedAttempts);

            var errorPolicyImplementation =
                context.Envelope.Endpoint.ErrorPolicy.Build(context.ServiceProvider);

            if (!errorPolicyImplementation.CanHandle(context, exception))
                return false;

            return await errorPolicyImplementation
                .HandleErrorAsync(context, exception)
                .ConfigureAwait(false);
        }
    }
}
