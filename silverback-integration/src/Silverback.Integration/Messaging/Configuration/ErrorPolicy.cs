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
        #region Chain

        /// <summary>
        /// Creates an <see cref="ErrorPolicyChain" />.
        /// </summary>
        /// <param name="policies">The policies to be applied one after the other.</param>
        /// <returns></returns>
        public static ErrorPolicyChain Chain(params ErrorPolicyBase[] policies)
            => new ErrorPolicyChain(policies);

        /// <summary>
        /// Creates an <see cref="ErrorPolicyChain" />.
        /// </summary>
        /// <param name="policies">The policies to be applied one after the other.</param>
        /// <returns></returns>
        public static ErrorPolicyChain Chain(IEnumerable<ErrorPolicyBase> policies)
            => new ErrorPolicyChain(policies);

        #endregion

        #region Retry

        /// <summary>
        /// Creates a <see cref="RetryErrorPolicy"/>.
        /// </summary>
        /// <param name="retryCount">The number of retry to be performed.</param>
        /// <param name="initialDelay">The time to wait in between the retries.</param>
        /// <param name="delayIncreament">Increase the delay at each retry.</param>
        public static RetryErrorPolicy Retry(int retryCount, TimeSpan? initialDelay = null,
            TimeSpan? delayIncreament = null)
            => new RetryErrorPolicy(retryCount, initialDelay, delayIncreament);

        #endregion

        #region Skip

        /// <summary>
        /// Creates a <see cref="SkipMessageErrorPolicy"/>.
        /// </summary>
        public static SkipMessageErrorPolicy Skip()
            => new SkipMessageErrorPolicy();

        #endregion

        #region Move

        /// <summary>
        /// Creates a <see cref="MoveMessageErrorPolicy" />.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <returns></returns>
        public static MoveMessageErrorPolicy Move(IEndpoint endpoint)
            => new MoveMessageErrorPolicy(endpoint);

        #endregion
    }
}
