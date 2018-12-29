using System;
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

        protected override void Produce(IMessage message, byte[] serializedMessage) =>
            File.WriteAllBytes(GetFilePath(message), serializedMessage);

        protected override async Task ProduceAsync(IMessage message, byte[] serializedMessage)
        {
            using (var stream = File.Open(GetFilePath(message), FileMode.Create))
            {
                await stream.WriteAsync(serializedMessage, 0, serializedMessage.Length);
            }
        }

        private string GetFilePath(IMessage message)
        {
            var fileName = message is IIntegrationMessage integrationMessage
                ? integrationMessage.Id.ToString()
                : "legacy-" + Guid.NewGuid().ToString();

            return Path.Combine(Endpoint.Path, $"{fileName}.txt");
        }
    }
}