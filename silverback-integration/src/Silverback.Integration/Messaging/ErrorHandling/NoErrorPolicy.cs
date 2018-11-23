using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy that isn't doing anything.
    /// </summary>
    internal class NoErrorPolicy : IErrorPolicy
    {
        private static NoErrorPolicy _instance;

        public static NoErrorPolicy Instance => _instance ?? (_instance = new NoErrorPolicy());

        public void TryHandleMessage(IEnvelope envelope, Action<IEnvelope> handler) => handler?.Invoke(envelope);
    }
}