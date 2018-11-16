using System;
using System.Text;

namespace Silverback
{
    /// <summary>
    /// Watches a folder and fires an event each time a new file is created.
    /// </summary>
    public abstract class FolderWatcher : IDisposable
    {
        public event EventHandler<string> FileCreated;

        protected void FireFileCreatedEvent(string path)
            => FileCreated?.Invoke(this, path);

        public abstract void Dispose();
    }
}
