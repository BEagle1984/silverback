using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class FileSystemConsumer : Consumer<FileSystemBroker, FileSystemEndpoint>, IDisposable
    {
        private FolderWatcher _watcher;

        public FileSystemConsumer(IBroker broker, IEndpoint endpoint, ILogger<FileSystemConsumer> logger)
            : base(broker, endpoint, logger)
        {
        }
        
        internal void Connect()
        {
            if (_watcher != null) throw new InvalidOperationException("Already connected");

            Endpoint.EnsurePathExists();

            _watcher = Endpoint.UseFileSystemWatcher
                ? (FolderWatcher) new FileSystemFolderWatcher(Endpoint.Path)
                : (FolderWatcher) new PollingFolderWatcher(Endpoint.Path);

            _watcher.FileCreated += (sender, path) =>
            {
                var acknowledged = false;

                while (!acknowledged)
                {
                    acknowledged = TryHandleMessage(path);

                    if (!acknowledged)
                        Thread.Sleep(100); // Wait a bit before a retry.
                }
            };
        }

        private bool TryHandleMessage(string path)
        {
            try
            {
                var buffer = ReadFileWithRetry(path);
                HandleMessage(buffer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal void Disconnect()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        private byte[] ReadFileWithRetry(string path)
        {
            int retry = 5;
            while (true)
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (IOException)
                {
                    if (retry == 0)
                        throw;
                }

                Thread.Sleep(50);
                retry--;
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}