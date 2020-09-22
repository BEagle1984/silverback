// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    public static class ErrorPolicy
    {
        public static StopConsumerErrorPolicy Stop() =>
            new StopConsumerErrorPolicy();

        public static SkipMessageErrorPolicy Skip() =>
            new SkipMessageErrorPolicy();

        public static RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
            new RetryErrorPolicy(initialDelay, delayIncrement);

        public static MoveMessageErrorPolicy Move(IProducerEndpoint endpoint) =>
            new MoveMessageErrorPolicy(endpoint);

        public static ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
            Chain(policies.AsEnumerable());

        public static ErrorPolicyChain Chain(IEnumerable<ErrorPolicyBase> policies) =>
            new ErrorPolicyChain(policies);
    }
}
