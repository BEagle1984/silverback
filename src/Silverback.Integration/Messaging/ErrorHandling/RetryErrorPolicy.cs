// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

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
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        public RetryErrorPolicy(IPublisher publisher, ILogger<RetryErrorPolicy> logger, MessageLogger messageLogger, TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null)
            : base(publisher, logger, messageLogger)
        {
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncrement = delayIncrement ?? TimeSpan.Zero;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        protected override ErrorAction ApplyPolicy(FailedMessage failedMessage, Exception exception)
        {
            ApplyDelay(failedMessage);

            _messageLogger.LogTrace(_logger, "The message will be processed again.", failedMessage);

            return ErrorAction.Retry;
        }

        private void ApplyDelay(FailedMessage failedMessage)
        {
            var delay = _initialDelay.Milliseconds + failedMessage.FailedAttempts * _delayIncrement.Milliseconds;

            if (delay <= 0)
                return;

            _messageLogger.LogTrace(_logger, $"Waiting {delay} milliseconds before retrying the message.", failedMessage);
            Thread.Sleep(delay);
        }
    }
}