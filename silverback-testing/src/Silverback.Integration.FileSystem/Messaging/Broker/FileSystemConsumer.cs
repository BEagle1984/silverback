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
    public class FileSystemConsumer : Consumer
    {
        private FileSystemWatcher _watcher;

        /// <summary>
        /// Gets the associated <see cref="T:Silverback.Messaging.Broker.IBroker" />.
        /// </summary>
        private new FileSystemBroker Broker => (FileSystemBroker)base.Broker;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemConsumer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        public FileSystemConsumer(IBroker broker, IEndpoint endpoint) 
            : base(broker, endpoint)
        {
        }

        internal void Connect()
        {
            if (_watcher != null) throw new InvalidOperationException("Already connected");

            var topicPath = Broker.GetTopicPath(Endpoint.Name);

            _watcher = new FileSystemWatcher(topicPath)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true
            };

            _watcher.Created += (sender, args) =>
            {
                //try
                {
                    var buffer = ReadFile(args.FullPath);
                    HandleMessage(buffer);
                }
                //catch
                {
                    // TODO: Logging and stuff but...what do?
                }
            };
        }

        internal void Disconnect()
        {
            _watcher?.Dispose();
            _watcher = null;
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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }

            base.Dispose(disposing);
        }
    }
}