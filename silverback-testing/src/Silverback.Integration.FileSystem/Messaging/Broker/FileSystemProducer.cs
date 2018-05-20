using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A file system bases <see cref="IProducer" />
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Producer" />
    /// <seealso cref="Silverback.Messaging.Broker.IProducer" />
    public class FileSystemProducer : Producer
    {
        /// <summary>
        /// Gets the associated <see cref="T:Silverback.Messaging.Broker.IBroker" />.
        /// </summary>
        private new FileSystemBroker Broker => (FileSystemBroker) base.Broker;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemProducer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        public FileSystemProducer(IBroker broker, IEndpoint endpoint) 
            : base(broker, endpoint)
        {
        }

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="serializedMessage">The serialized <see cref="T:Silverback.Messaging.Messages.IEnvelope" /> including the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" />.
        /// This is what is supposed to be sent through the broker.</param>
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        =>   File.WriteAllBytes(GetFilePath(message), serializedMessage);

        protected override Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage)
        {
            using (var stream = File.Open(GetFilePath(message), FileMode.Create))
            {
                return stream.WriteAsync(serializedMessage, 0, serializedMessage.Length);
            }
        }

        /// <summary>
        /// Resolves the full target path of the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private string GetFilePath(IIntegrationMessage message)
            => Path.Combine(Broker.GetTopicPath(Endpoint.Name), $"{message.Id}.txt");
    }
}