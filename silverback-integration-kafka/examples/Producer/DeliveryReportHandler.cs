using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Confluent.Kafka;

namespace Producer
{
    public class DeliveryReportHandler : Silverback.Messaging.DeliveryReportHandler
    {
        private readonly string  _filePath;

        public DeliveryReportHandler(string file)
        {
            _filePath = file;
        }

        public override void HandleDeliveryReport(Message<byte[], byte[]> message)
        {
            using (var writer = new StreamWriter(_filePath, append: true))
            {
                writer.WriteLine($"{message.Timestamp.UtcDateTime.ToLocalTime()} - Topic: {message.Topic} Outcome: {message.Error.Reason}");
            }                        
        }
    }
}
 