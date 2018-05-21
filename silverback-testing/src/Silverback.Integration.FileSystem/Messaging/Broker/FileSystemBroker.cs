using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A message broker implementation based on files.
    /// </summary>
    public class FileSystemBroker : Broker
    {
        /// <summary>
        /// Gets the path of the folder used to exchange the messages.
        /// </summary>
        /// <value>
        /// The base path.
        /// </value>
        public string Path { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a <see cref="System.IO.FileSystemWatcher"/> is used
        /// to monitor the topic folder.
        /// </summary>
        public bool IsUsingFileSystemWatcher { get; private set; }

        #region Configuration

        /// <summary>
        /// Sets the path of the folder used to exchange the messages.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public FileSystemBroker OnPath(string path)
        {
            Path = path;
            return this;
        }

        /// <summary>
        /// Configures the broker to use a <see cref="System.IO.FileSystemWatcher"/> to monitor the topic folders.
        /// </summary>
        /// <returns></returns>
        public FileSystemBroker UseFileSystemWatcher()
        {
            IsUsingFileSystemWatcher = true;
            return this;
        }

        /// <summary>
        /// Called after the fluent configuration is applied, should verify the consistency of the
        /// configuration.
        /// </summary>
        public override void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(Path)) throw new InvalidOperationException("The BasePath must be set.");
            if (!Directory.Exists(Path)) throw new InvalidOperationException("The BasePath must be set to an existing directory path.");
        }

        #endregion

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.Producer" /> instance to publish messages to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override Producer GetNewProducer(IEndpoint endpoint)
            => new FileSystemProducer(this, endpoint);

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.Consumer" /> instance to listen to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override Consumer GetNewConsumer(IEndpoint endpoint)
            => new FileSystemConsumer(this, endpoint, IsUsingFileSystemWatcher);

        /// <summary>
        /// Connects the specified consumers.
        /// After a successful call to this method the consumers will start listening to
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected override void Connect(IEnumerable<IConsumer> consumers)
            => consumers.Cast<FileSystemConsumer>().ToList().ForEach(c => c.Connect());

        /// <summary>
        /// Connects the specified producers.
        /// After a successful call to this method the producers will be ready to send messages.
        /// </summary>
        /// <param name="producers">The producers.</param>
        protected override void Connect(IEnumerable<IProducer> producers)
        {
        }

        /// <summary>
        /// Disconnects the specified consumers. The consumers will not receive any further
        /// message.
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected override void Disconnect(IEnumerable<IConsumer> consumers)
            => consumers.Cast<FileSystemConsumer>().ToList().ForEach(c => c.Disconnect());

        /// <summary>
        /// Disconnects the specified producers. The producers will not be able to send messages anymore.
        /// </summary>
        /// <param name="producer">The producer.</param>
        protected override void Disconnect(IEnumerable<IProducer> producer)
        {
        }

        /// <summary>
        /// Gets the physical path of the specified topic.
        /// </summary>
        /// <param name="topicName">The name of the topic.</param>
        /// <param name="create">if set to <c>true</c> the folder will be created if not existing already.</param>
        /// <returns></returns>
        internal string GetTopicPath(string topicName, bool create = true)
        {
            var topicPath = System.IO.Path.Combine(Path, topicName);

            if (create && !Directory.Exists(topicPath))
                Directory.CreateDirectory(topicPath);

            return topicPath;
        }
    }
}
