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
        private readonly ILogger<RetryErrorPolicy> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _delayIncreament;

        public RetryErrorPolicy(ILogger<RetryErrorPolicy> logger, int maxRetries = -1, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
            : base(logger)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncreament = delayIncreament ?? TimeSpan.Zero;
        }

        protected override void ApplyPolicy(IMessage message, Action<IMessage> messageHandler, Exception exception)
        {
            var delay = _initialDelay;

            var i = 1;

            while (_maxRetries < 0 || i <= _maxRetries) 
            {
                if (delay != TimeSpan.Zero)
                {
                    Thread.Sleep(delay);

                    if (_delayIncreament != TimeSpan.Zero)
                        delay = delay + _delayIncreament;
                }

                try
                {
                    messageHandler.Invoke(message);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == _maxRetries || !MustHandle(ex)) // TODO: Is it correct to reevaluate the exception type?
                        throw;

                    // TODO: Is this really to be a warning?
                    _logger.LogWarning(ex, $"An error occurred retrying the message {message.GetTraceString()}. " +
                                           $"Will try again.");
                }

                i++;
            }
        }
    }
}