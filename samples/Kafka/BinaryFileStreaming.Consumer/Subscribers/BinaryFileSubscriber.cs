using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Subscribers
{
    public class BinaryFileSubscriber
    {
        private const string OutputPath = "../../temp";

        private readonly Random _random;

        private readonly ILogger<BinaryFileSubscriber> _logger;

        public BinaryFileSubscriber(ILogger<BinaryFileSubscriber> logger)
        {
            _random = new Random((int)DateTime.Now.Ticks);
            _logger = logger;
        }

        public async Task OnBinaryFileMessageReceived(
            CustomBinaryFileMessage binaryFileMessage)
        {
            EnsureTargetFolderExists();

            var filename = Guid.NewGuid().ToString("N") + binaryFileMessage.Filename;

            _logger.LogInformation($"Saving binary file as {filename}...");

            // Create a FileStream to save the file
            using var fileStream = File.OpenWrite(Path.Combine(OutputPath, filename));

            if (binaryFileMessage.Content != null)
            {
                // Asynchronously copy the message content to the FileStream.
                // The message chunks are streamed directly and the entire file is
                // never loaded into memory.
                await binaryFileMessage.Content.CopyToAsync(fileStream);
            }

            // Randomly simulate a processing exception
            if (_random.Next(2) == 1)
                throw new InvalidOperationException("You must fail!");

            _logger.LogInformation(
                $"Written {fileStream.Length} bytes into {filename}.");
        }

        private static void EnsureTargetFolderExists()
        {
            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);
        }
    }
}
