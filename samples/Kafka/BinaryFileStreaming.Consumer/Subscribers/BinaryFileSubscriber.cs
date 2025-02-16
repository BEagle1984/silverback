using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Subscribers;

public class BinaryFileSubscriber
{
    private const string OutputPath = "../../temp";

    private readonly ILogger<BinaryFileSubscriber> _logger;

    public BinaryFileSubscriber(ILogger<BinaryFileSubscriber> logger)
    {
        _logger = logger;
    }

    public async Task OnBinaryMessageReceivedAsync(CustomBinaryMessage binaryMessage)
    {
        EnsureTargetFolderExists();

        string filename = Guid.NewGuid().ToString("N") + binaryMessage.Filename;

        _logger.LogInformation("Saving binary file as {Filename}...", filename);

        // Create a FileStream to save the file
        using FileStream fileStream =
            File.OpenWrite(Path.Combine(OutputPath, filename));

        if (binaryMessage.Content != null)
        {
            // Asynchronously copy the message content to the FileStream.
            // The message chunks are streamed directly and the entire file is
            // never loaded into memory.
            await binaryMessage.Content.CopyToAsync(fileStream);
        }

        _logger.LogInformation(
            "Written {FileStreamLength} bytes into {Filename}",
            fileStream.Length,
            filename);
    }

    private static void EnsureTargetFolderExists()
    {
        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);
    }
}
