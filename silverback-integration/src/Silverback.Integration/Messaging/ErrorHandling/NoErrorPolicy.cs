using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy that isn't doing anything but logging.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    internal class NoErrorPolicy : IErrorPolicy
    {
        /// <summary>
        /// Wraps the specified policy.
        /// </summary>
        /// <param name="policy">The policy to be executed if this one fails.</param>
        /// <exception cref="NotImplementedException"></exception>
        public IErrorPolicy Wrap(IErrorPolicy policy)
        {
            throw new NotSupportedException("Chaining is not supported by the NoErrorPolicy.");
        }

        /// <summary>
        /// Tries to process the message with the specified handler and takes care of handling
        /// the possible errors.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="handler">The method that handles the message.</param>
        public void TryHandleMessage<T>(T message, Action<T> handler) where T : IIntegrationMessage
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            try
            {
                handler.Invoke(message);
            }
            catch (Exception)
            {
                // TODO: Log & Trace
                throw;
            }
        }
    }
}