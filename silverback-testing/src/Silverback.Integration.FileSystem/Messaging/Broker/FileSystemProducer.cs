using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class FileSystemProducer : Producer<FileSystemBroker, FileSystemEndpoint>
    {
        public FileSystemProducer(IBroker broker, IEndpoint endpoint, ILogger<Producer> logger)
            : base(broker, endpoint, logger)
        {
            Endpoint.EnsurePathExists();
        }

        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage) =>
            File.WriteAllBytes(GetFilePath(message), serializedMessage);

        protected override Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage)
        {
            using (var stream = File.Open(GetFilePath(message), FileMode.Create))
            {
                return stream.WriteAsync(serializedMessage, 0, serializedMessage.Length);
            }
        }

        private string GetFilePath(IIntegrationMessage message) => Path.Combine(Endpoint.Path, $"{message.Id}.txt");
    }
}