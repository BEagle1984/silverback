using System;
using System.IO;
using System.Threading;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A file system based <see cref="IConsumer" /> implementation.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Consumer" />
    /// <seealso cref="Silverback.Messaging.Broker.IConsumer" />
    /// <seealso cref="System.IDisposable" />
    public class FileSystemConsumer : Consumer, IDisposable
    {
        private readonly FileSystemBroker _broker;
        private FileSystemWatcher _watcher;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemConsumer"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public FileSystemConsumer(IEndpoint endpoint) 
            : base(endpoint)
        {
            _broker = endpoint.GetBroker<FileSystemBroker>();
        }

        /// <summary>
        /// Start listening to the specified enpoint and consume the messages delivered
        /// through the message broker.
        /// </summary>
        /// <param name="handler">The handler.</param>
        protected override void Consume(Action<byte[]> handler)
        {
            var topicPath = _broker.GetTopicPath(Endpoint.Name);

            _watcher = new FileSystemWatcher(topicPath)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true
            };

            _watcher.Created += (sender, args) =>
            {
                //try
                {
                    handler.Invoke(ReadFile(args.FullPath));
                }
                //catch
                {
                    // TODO: Logging and stuff but...what do?
                }
            };
        }

        /// <summary>
        /// Reads the file retrying 5 times (try to avoid concurrency issues).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private byte[] ReadFile(string path)
        {
            int retry = 5;
            while (true)
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (IOException ex)
                {
                    if (retry == 0)
                        throw ex;
                }

                Thread.Sleep(50);
                retry--;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}