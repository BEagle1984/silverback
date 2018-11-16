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
        private readonly ILogger<RetryErrorPolicy> _logger;
        private readonly int _retryCount;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _delayIncreament;

        public RetryErrorPolicy(ILogger<RetryErrorPolicy> logger, int retryCount, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
            : base(logger)
        {
            if (retryCount <= 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "Specify a retry count greater than 0.");

            _logger = logger;
            _retryCount = retryCount;
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncreament = delayIncreament ?? TimeSpan.Zero;
        }

        protected override void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
        {
            var delay = _initialDelay;

            for (var i = 1; i <= _retryCount; i++)
            {
                if (delay != TimeSpan.Zero)
                {
                    Thread.Sleep(delay);

                    if (_delayIncreament != TimeSpan.Zero)
                        delay = delay + _delayIncreament;
                }

                try
                {
                    handler.Invoke(envelope);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == _retryCount || !MustHandle(ex)) // TODO: Is it correct to reevaluate the exception type?
                        throw;

                    // TODO: Is this really to be a warning?
                    _logger.LogWarning(ex, $"An error occurred retrying the message '{envelope.Message.Id}'. " +
                                           $"Will try again.");
                }
            }
        }
    }
}