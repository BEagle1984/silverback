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
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public void Init(IBus bus)
        {
        }

        /// <summary>
        /// Tries to process the message with the specified handler and takes care of handling
        /// the possible errors.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be handled.</param>
        /// <param name="handler">The method that handles the message.</param>
        /// <exception cref="ArgumentNullException">handler</exception>
        public void TryHandleMessage(IEnvelope envelope, Action<IEnvelope> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            handler.Invoke(envelope);
        }
    }
}