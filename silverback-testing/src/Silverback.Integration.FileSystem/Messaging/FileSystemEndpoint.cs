using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Silverback.Messaging
{
    public class FileSystemEndpoint : IEndpoint
    {
        private FileSystemEndpoint(string name, string basePath, bool useFileSystemWatcher)
        {
            Name = name;
            UseFileSystemWatcher = useFileSystemWatcher;
            Path = System.IO.Path.Combine(basePath, name);
        }

        public static FileSystemEndpoint Create(string name, string basePath, bool useFileSystemWatcher = false) =>
            new FileSystemEndpoint(name, basePath, useFileSystemWatcher);

        public string Name { get; }

        public string Path { get; }

        public bool UseFileSystemWatcher { get; }

        internal void EnsurePathExists()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }
    }
}
