using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A message broker implementation based on files.
    /// </summary>
    public class FileSystemBroker : Broker
    {
        public string BasePath { get; private set; }

        #region Configuration

        /// <summary>
        /// Sets the base path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public FileSystemBroker OnPath(string path)
        {
            BasePath = path;
            return this;
        }

        /// <summary>
        /// Called after the fluent configuration is applied, should verify the consistency of the
        /// configuration.
        /// </summary>
        public override void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(BasePath)) throw new InvalidOperationException("The BasePath must be set.");
            if (!Directory.Exists(BasePath)) throw new InvalidOperationException("The BasePath must be set to an existing directory path.");
        }

        #endregion

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.IProducer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override IProducer GetProducer(IEndpoint endpoint)
            => new FileSystemProducer(endpoint);

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.IConsumer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override IConsumer GetConsumer(IEndpoint endpoint)
            => new FileSystemConsumer(endpoint);

        /// <summary>
        /// Gets the physical path of the specified topic.
        /// </summary>
        /// <param name="topicName">The name of the topic.</param>
        /// <param name="create">if set to <c>true</c> the folder will be created if not existing already.</param>
        /// <returns></returns>
        public string GetTopicPath(string topicName, bool create = true)
        {
            var topicPath = Path.Combine(BasePath, topicName);

            if (create && !Directory.Exists(topicPath))
                Directory.CreateDirectory(topicPath);

            return topicPath;
        }
    }
}
