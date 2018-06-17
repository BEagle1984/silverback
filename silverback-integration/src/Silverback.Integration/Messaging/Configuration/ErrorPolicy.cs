using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Helper methods to configure the error policies to be applied when 
    /// processing the inbound messages.
    /// </summary>
    public static class ErrorPolicy
    {
        /// <summary>
        /// Configures a <see cref="RetryErrorPolicy"/>.
        /// </summary>
        /// <param name="retryCount">The number of retry to be performed.</param>
        /// <param name="initialDelay">The time to wait in between the retries.</param>
        /// <param name="delayIncreament">Increase the delay at each retry.</param>
        public static RetryErrorPolicy Retry(int retryCount, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
        {
            return new RetryErrorPolicy(retryCount, initialDelay, delayIncreament);
        }

        /// <summary>
        /// Adds a <see cref="RetryErrorPolicy" /> to the error handling pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the parent policy.</typeparam>
        /// <param name="policy">The policy that will wrap the new policy.</param>
        /// <param name="retryCount">The number of retry to be performed.</param>
        /// <param name="initialDelay">The time to wait in between the retries.</param>
        /// <param name="delayIncreament">Increase the delay at each retry.</param>
        /// <returns></returns>
        public static T Retry<T>(this T policy, int retryCount, TimeSpan? initialDelay = null, TimeSpan? delayIncreament = null)
            where T : RetryErrorPolicy
        {
            policy.Wrap(new RetryErrorPolicy(retryCount, initialDelay, delayIncreament));

            return policy;

        }
    }
}
