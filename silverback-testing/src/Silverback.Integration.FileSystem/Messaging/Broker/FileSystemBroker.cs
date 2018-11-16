using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A message broker implementation based on files.
    /// </summary>
    public class FileSystemBroker : Broker<FileSystemEndpoint>
    {

        public FileSystemBroker(IMessageSerializer serializer, ILoggerFactory loggerFactory)
            : base(serializer, loggerFactory)
        {
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint) => new FileSystemProducer(this, endpoint, LoggerFactory.CreateLogger<FileSystemProducer>());

        protected override Consumer InstantiateConsumer(IEndpoint endpoint) => new FileSystemConsumer(this, endpoint, LoggerFactory.CreateLogger<FileSystemConsumer>());

        protected override void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<FileSystemConsumer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
            => consumers.Cast<FileSystemConsumer>().ToList().ForEach(c => c.Disconnect());
    }
}
