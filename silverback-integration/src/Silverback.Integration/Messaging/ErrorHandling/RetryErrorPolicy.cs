using System;
using System.Threading;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy retries the handler method multiple times in case of exception.
    /// An optional delay can be specified.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    /// TODO: Exponential backoff variant
    public class RetryErrorPolicy : ErrorPolicyBase
    {
        private readonly int _retryCount;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _delayIncreament;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryErrorPolicy"/> class.
        /// </summary>
        /// <param name="retryCount">The number of retry to be performed.</param>
        /// <param name="initialDelay">The time to wait in between the retries.</param>
        /// <param name="delayIncreament">Increase the delay at each retry.</param>
        /// <exception cref="ArgumentOutOfRangeException">retryCount - Specify a retry count greater than 0.</exception>
        public RetryErrorPolicy(int retryCount, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
        {
            if (retryCount <= 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "Specify a retry count greater than 0.");

            _retryCount = retryCount;
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncreament = delayIncreament ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        protected override void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler)
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
                catch (Exception)
                {
                    // TODO: Log & Trace

                    if (i == _retryCount)
                        throw;
                }
            }
        }
    }
}