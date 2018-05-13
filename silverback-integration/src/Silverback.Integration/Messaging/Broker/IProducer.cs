using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// Exposes the methods to send messages through over the message broker.
    /// </summary>
    public interface IProducer
    {
        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be sent.</param>
        void Produce(IEnvelope envelope);

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be sent.</param>
        Task ProduceAsync(IEnvelope envelope);
    }
}