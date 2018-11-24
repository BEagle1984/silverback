using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public class FileSystemEndpoint : IEndpoint, IEquatable<FileSystemEndpoint>
    {
        public FileSystemEndpoint(string name, string basePath)
        {
            Name = name;
            BasePath = basePath;
            Path = System.IO.Path.Combine(BasePath, Name);
        }

        public string Name { get; }

        public string BasePath { get; }

        [JsonIgnore]
        public string Path { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="FileSystemWatcher"/> is to be used 
        /// to monitor the topic folder instead of polling it. (default=false)
        /// </summary>
        /// <remarks>The <see cref="FileSystemWatcher" /> doesn't seem to work from within docker (for windows) on a shared drive.</remarks>
        public bool UseFileSystemWatcher { get; set; }

        internal void EnsurePathExists()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }

        #region IEquatable

        public bool Equals(FileSystemEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && string.Equals(Path, other.Path) && UseFileSystemWatcher == other.UseFileSystemWatcher;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileSystemEndpoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
