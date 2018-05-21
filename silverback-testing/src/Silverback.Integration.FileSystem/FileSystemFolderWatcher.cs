using System.IO;

namespace Silverback
{
    /// <summary>
    /// Uses a <see cref="FileSystemWatcher"/> to monitor a folder and fire an event each time a new file is created.
    /// </summary>
    /// <seealso cref="Silverback.FolderWatcher" />
    public class FileSystemFolderWatcher : FolderWatcher
    {
        private FileSystemWatcher _watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemFolderWatcher"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        public FileSystemFolderWatcher(string folderPath)
        {
            _watcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true
            };

            _watcher.Created += (sender, args) => FireFileCreatedEvent(args.FullPath);
        }

        public override void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}