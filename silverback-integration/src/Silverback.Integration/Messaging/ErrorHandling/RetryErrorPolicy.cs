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
    /// TODO: Test maxRetries = -1
    public class RetryErrorPolicy : ErrorPolicyBase
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _delayIncreament;

        public RetryErrorPolicy(ILogger<RetryErrorPolicy> logger, int maxRetries = -1, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
            : base(logger)
        {
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncreament = delayIncreament ?? TimeSpan.Zero;
        }

        public override bool CanHandle(IMessage failedMessage, int retryCount, Exception exception) =>
            base.CanHandle(failedMessage, retryCount, exception) && retryCount < _maxRetries;

        public override ErrorAction HandleError(IMessage failedMessage, int retryCount, Exception exception)
        {
            ApplyDelay(retryCount);

            return ErrorAction.RetryMessage;
        }

        private void ApplyDelay(int retryCount)
        {
            var delay = _initialDelay.Milliseconds + retryCount * _delayIncreament.Milliseconds;

            if (delay > 0) Thread.Sleep(delay);
        }
    }
}