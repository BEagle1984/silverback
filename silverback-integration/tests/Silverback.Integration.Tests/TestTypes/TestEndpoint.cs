using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging;

namespace Silverback.Tests.TestTypes
{
    public sealed class TestEndpoint : IEndpoint, IEquatable<TestEndpoint>
    {
        /// <summary>
        /// Gets or sets the topic/queue name.
        /// </summary>
        public string Name { get; }

        [JsonConstructor]
        private TestEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a new <see cref="BasicEndpoint" /> pointing to the specified topic/queue.
        /// </summary>
        /// <param name="name">The queue/topic name.</param>
        /// <returns></returns>
        public static TestEndpoint Create(string name)
            => new TestEndpoint(name);

        public static TestEndpoint Default = Create("test");

        #region IEquatable

        public bool Equals(TestEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TestEndpoint && Equals((TestEndpoint)obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        #endregion
    }
}
