// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class ConfigurationReaderTests
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
        }

        #region Read - Inbound

        [Test]
        public void Read_SimplestInbound_EndpointAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest", "Silverback"));

            Assert.AreEqual(1, reader.Inbound.Count);
            Assert.IsNotNull(reader.Inbound.First().Endpoint);
        }

        [Test]
        public void Read_SimplestInbound_CorrectEndpointType()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest", "Silverback"));

            var endpoint = reader.Inbound.First().Endpoint;
            Assert.IsInstanceOf<KafkaConsumerEndpoint>(endpoint);
        }

        [Test]
        public void Read_SimplestInbound_EndpointNameSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest", "Silverback"));

            var endpoint = reader.Inbound.First().Endpoint;
            Assert.AreEqual("inbound-endpoint", endpoint.Name);
        }

        [Test]
        public void Read_SimplestInbound_DefaultSerializer()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest", "Silverback"));

            var endpoint = reader.Inbound.First().Endpoint;
            Assert.IsNotNull(endpoint.Serializer);
        }

        [Test]
        public void Read_CompleteInbound_EndpointPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var endpoint = (KafkaConsumerEndpoint) reader.Inbound.First().Endpoint;
            Assert.AreEqual(1234, endpoint.CommitOffsetEach);
        }

        [Test]
        public void Read_CompleteInbound_EndpointSubPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var endpoint = (KafkaConsumerEndpoint)reader.Inbound.First().Endpoint;

            // Note: Confluent.Kafka currently has a bug preventing the property
            // value to be retrieved
            Assert.Throws<ArgumentException>(
                () => Assert.AreEqual(AutoOffsetResetType.Earliest, endpoint.Configuration.AutoOffsetReset),
                "Requested value 'earliest' was not found.");
        }

        #endregion
    }
}
