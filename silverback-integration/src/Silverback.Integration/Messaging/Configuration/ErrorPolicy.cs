using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test (?)
    /// <summary>
    /// Helper methods to configure the error policies to be applied when 
    /// processing the inbound messages.
    /// </summary>
    public static class ErrorPolicy
    {
        #region Retry

        /// <summary>
        /// Configures a <see cref="RetryErrorPolicy"/>.
        /// </summary>
        /// <param name="retryCount">The number of retry to be performed.</param>
        /// <param name="initialDelay">The time to wait in between the retries.</param>
        /// <param name="delayIncreament">Increase the delay at each retry.</param>
        public static RetryErrorPolicy Retry(int retryCount, TimeSpan? initialDelay = null,
            TimeSpan? delayIncreament = null)
            => new RetryErrorPolicy(retryCount, initialDelay, delayIncreament);

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
            where T : IErrorPolicy
            => (T) policy.Wrap(Retry(retryCount, initialDelay, delayIncreament));

        #endregion

        #region Skip

        /// <summary>
        /// Configures a <see cref="SkipMessageErrorPolicy"/>.
        /// </summary>
        public static SkipMessageErrorPolicy Skip()
            => new SkipMessageErrorPolicy();

        /// <summary>
        /// Adds a <see cref="SkipMessageErrorPolicy" /> to the error handling pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the parent policy.</typeparam>
        /// <param name="policy">The policy that will wrap the new policy.</param>
        /// <returns></returns>
        public static T Skip<T>(this T policy)
            where T : IErrorPolicy
            => (T)policy.Wrap(Skip());

        #endregion

        #region Move

        /// <summary>
        /// Configures a <see cref="MoveMessageErrorPolicy" />.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <returns></returns>
        public static MoveMessageErrorPolicy Move(IEndpoint endpoint)
            => new MoveMessageErrorPolicy(endpoint);

        /// <summary>
        /// Adds a <see cref="SkipMessageErrorPolicy" /> to the error handling pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the parent policy.</typeparam>
        /// <param name="policy">The policy that will wrap the new policy.</param>
        /// <param name="endpoint">The target endpoint.</param>
        /// <returns></returns>
        public static T Move<T>(this T policy, IEndpoint endpoint)
            where T : IErrorPolicy
            => (T)policy.Wrap(Move(endpoint));

        #endregion
    }
}
