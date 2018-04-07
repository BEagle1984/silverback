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
        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
        }

        [Test]
        public void CreateTest()
        {
            var endpoint = BasicEndpoint.Create("untitest");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.Name, Is.EqualTo("untitest"));
        }

        [Test]
        public void GetBrokerTest()
        {
            BrokersConfig.Instance
                .Add<TestBroker>(c => c.UseServer("server1").WithName("broker1"))
                .Add<TestBroker>(c => c.UseServer("server2").WithName("broker2"));

            var endpoint = BasicEndpoint.Create("test").UseBroker("broker2");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.GetBroker(), Is.Not.Null);
            Assert.That(endpoint.GetBroker().Name, Is.EqualTo("broker2"));
        }

        [Test]
        public void GetBrokerTypedTest()
        {
            BrokersConfig.Instance
                .Add<TestBroker>(c => c.UseServer("server1").WithName("broker1"))
                .Add<TestBroker>(c => c.UseServer("server2").WithName("broker2"));

            var endpoint = BasicEndpoint.Create("test").UseBroker("broker2");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.GetBroker(), Is.Not.Null);
            Assert.That(endpoint.GetBroker<TestBroker>().ServerName, Is.EqualTo("server2"));
        }

        [Test]
        public void GetDefaultBrokerConfigurationTest()
        {
            BrokersConfig.Instance
                .Add<TestBroker>(c => c.UseServer("server1").WithName("broker1"))
                .Add<TestBroker>(c => c.UseServer("server2").WithName("broker2").AsDefault());

            var endpoint = BasicEndpoint.Create("test");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.GetBroker(), Is.Not.Null);
            Assert.That(endpoint.GetBroker().Name, Is.EqualTo("broker2"));
        }
    }
}
