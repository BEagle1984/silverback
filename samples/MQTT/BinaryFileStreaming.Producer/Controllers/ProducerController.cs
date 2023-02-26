using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Mqtt.BinaryFileStreaming.Producer.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Producer.Controllers;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly IPublisher _publisher;

    public ProducerController(IPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("binary-file")]
    public async Task<IActionResult> ProduceBinaryFileAsync(
        string filePath,
        string? contentType)
    {
        // Open specified file stream
        await using FileStream fileStream = System.IO.File.OpenRead(filePath);

        // Create a BinaryMessage that wraps the file stream
        BinaryMessage binaryMessage = new(fileStream);

        if (!string.IsNullOrEmpty(contentType))
            binaryMessage.ContentType = contentType;

        // Publish the BinaryFileMessage that will be routed to the outbound
        // endpoint. The FileStream will be read and produced chunk by chunk,
        // without the entire file being loaded into memory.
        await _publisher.PublishAsync(binaryMessage);

        return NoContent();
    }

    [HttpPost("custom-binary-file")]
    public async Task<IActionResult> ProduceBinaryFileWithCustomHeadersAsync(
        string filePath,
        string? contentType)
    {
        // Open specified file stream
        await using FileStream fileStream = System.IO.File.OpenRead(filePath);

        // Create a CustomBinaryFileMessage that wraps the file stream. The
        // CustomBinaryFileMessage extends the BinaryFileMessage adding an extra
        // Filename property that is also exported as header.
        CustomBinaryFileMessage binaryFileMessage = new()
        {
            Content = fileStream,
            Filename = Path.GetFileName(filePath)
        };

        if (!string.IsNullOrEmpty(contentType))
            binaryFileMessage.ContentType = contentType;

        // Publish the BinaryFileMessage that will be routed to the outbound
        // endpoint. The FileStream will be read and produced chunk by chunk,
        // without the entire file being loaded into memory.
        await _publisher.PublishAsync(binaryFileMessage);

        return NoContent();
    }
}
