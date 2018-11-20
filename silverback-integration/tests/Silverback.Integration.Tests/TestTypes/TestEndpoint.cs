using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public sealed class TestEndpoint : IEndpoint, IEquatable<TestEndpoint>
    {
        [JsonConstructor]
        private TestEndpoint(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

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
