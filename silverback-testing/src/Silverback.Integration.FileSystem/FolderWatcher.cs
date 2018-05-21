using System;
using System.Text;

namespace Silverback
{
    /// <summary>
    /// Watches a folder and fires an event each time a new file is created.
    /// </summary>
    public abstract class FolderWatcher : IDisposable
    {
        /// <summary>
        /// Occurs when a new file is created.
        /// </summary>
        public event EventHandler<string> FileCreated;

        /// <summary>
        /// Fires the file created event.
        /// </summary>
        /// <param name="path">The file path.</param>
        protected void FireFileCreatedEvent(string path)
            => FileCreated?.Invoke(this, path);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public abstract void Dispose();
    }
}
