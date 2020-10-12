// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Samples.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.BinaryFileStreaming.Consumer.Subscribers
{
    public class BinaryFileSubscriber
    {
        private readonly ILogger<BinaryFileSubscriber> _logger;

        public BinaryFileSubscriber(ILogger<BinaryFileSubscriber> logger)
        {
            _logger = logger;
        }

        public async Task OnBinaryFileMessageReceived(CustomBinaryFileMessage binaryFileMessage)
        {
            var filename = Guid.NewGuid().ToString("N") + binaryFileMessage.Filename;

            _logger.LogInformation($"Saving binary file as {filename}...");

            // Create a FileStream to save the file
            using var fileStream = File.OpenWrite(filename);

            if (binaryFileMessage.Content != null)
            {
                // Asynchronously copy the message content to the FileStream. The message chunks are streamed directly
                // and the entire file is never loaded into memory.
                await binaryFileMessage.Content.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Written {fileStream.Length} bytes into {filename}.");
        }
    }
}
