using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class BasicEndpointTests
    {
        [Test]
        public void CreateTest()
        {
            var endpoint = BasicEndpoint.Create("untitest");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.Name, Is.EqualTo("untitest"));
        }

        [Test]
        [TestCase("Test", "Test", true)]
        [TestCase("Test", "Other", false)]
        [TestCase("Test", "test", true)]
        public void EqualsTest(string name1, string name2, bool areEqual)
        {
            var endpoint1 = BasicEndpoint.Create(name1);
            var endpoint2 = BasicEndpoint.Create(name2);

            Assert.That(endpoint1.Equals(endpoint2), Is.EqualTo(areEqual));
        }

        [Test]
        [TestCase("Test", "Test", true)]
        [TestCase("Test", "Other", false)]
        [TestCase("Test", "test", true)]
        public void EqualsObjectTest(string name1, string name2, bool areEqual)
        {
            object endpoint1 = BasicEndpoint.Create(name1);
            object endpoint2 = BasicEndpoint.Create(name2);

            Assert.That(endpoint1.Equals(endpoint2), Is.EqualTo(areEqual));
        }
    }
}
