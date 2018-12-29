// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy retries the handler method multiple times in case of exception.
    /// An optional delay can be specified.
    /// </summary>
    /// TODO: Exponential backoff variant
    public class RetryErrorPolicy : ErrorPolicyBase
    {
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _delayIncrement;

        public RetryErrorPolicy(ILogger<RetryErrorPolicy> logger, TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null)
            : base(logger)
        {
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncrement = delayIncrement ?? TimeSpan.Zero;
        }

        public override ErrorAction HandleError(FailedMessage failedMessage, Exception exception)
        {
            ApplyDelay(failedMessage.FailedAttempts);

            return ErrorAction.RetryMessage;
        }

        private void ApplyDelay(int failedAttempts)
        {
            var delay = _initialDelay.Milliseconds + failedAttempts * _delayIncrement.Milliseconds;

            if (delay > 0) Thread.Sleep(delay);
        }
    }
}