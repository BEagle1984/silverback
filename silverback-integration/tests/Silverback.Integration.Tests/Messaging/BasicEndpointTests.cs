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
    }
}
