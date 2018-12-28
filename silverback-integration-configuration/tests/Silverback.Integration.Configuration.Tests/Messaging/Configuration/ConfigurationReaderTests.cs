// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;

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

            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton(Substitute.For<IBroker>());

            _serviceProvider = services.BuildServiceProvider();
        }

        #region Read - Inbound

        [Test]
        public void Read_SimplestInbound_EndpointAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest", "Silverback"));

            Assert.AreEqual(2, reader.Inbound.Count);
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

            var endpoint = reader.Inbound.Skip(1).First().Endpoint;
            Assert.AreEqual("inbound-endpoint2", endpoint.Name);
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
        public void Read_CompleteInbound_DefaultSerializerPropertiesSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var serializer = (JsonMessageSerializer)reader.Inbound.Skip(1).First().Endpoint.Serializer;
            Assert.AreEqual(MessageEncoding.Unicode, serializer.Encoding);
            Assert.AreEqual(Formatting.Indented, serializer.Settings.Formatting);
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

        [Test]
        public void Read_CompleteInbound_CustomSerializerSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var endpoint = reader.Inbound.First().Endpoint;
            Assert.IsInstanceOf<FakeSerializer>(endpoint.Serializer);
        }

        [Test]
        public void Read_CompleteInbound_CustomSerializerPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var serializer = (FakeSerializer) reader.Inbound.First().Endpoint.Serializer;
            Assert.AreEqual(4, serializer.Settings.Mode);
        }

        [Test]
        public void Read_CompleteInbound_ErrorPoliciesAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            Assert.AreEqual(2, reader.Inbound.First().ErrorPolicies.Count());
        }

        [Test]
        public void Read_CompleteInbound_ErrorPolicyMaxFailedAttemptsSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            Assert.IsTrue(policy.CanHandle(new FailedMessage(null, 3), new ArgumentException()));
            Assert.IsFalse(policy.CanHandle(new FailedMessage(null, 6), new ArgumentException()));
        }
        
        [Test]
        public void Read_CompleteInbound_ErrorPolicyConstructorParameterSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var policy = (RetryErrorPolicy) reader.Inbound.First().ErrorPolicies.First();
            Assert.AreEqual(TimeSpan.FromMinutes(5), (TimeSpan)policy.GetType().GetField("_delayIncrement", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(policy));
        }

        [Test]
        public void Read_CompleteInbound_ErrorPolicyApplyToSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            Assert.IsTrue(policy.CanHandle(new FailedMessage(), new ArgumentException()));
            Assert.IsTrue(policy.CanHandle(new FailedMessage(), new InvalidOperationException()));
            Assert.IsFalse(policy.CanHandle(new FailedMessage(), new FormatException()));
        }

        [Test]
        public void Read_CompleteInbound_ErrorPolicyExcludeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete", "Silverback"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            Assert.IsTrue(policy.CanHandle(new FailedMessage(), new ArgumentException()));
            Assert.IsFalse(policy.CanHandle(new FailedMessage(), new ArgumentNullException()));
        }

        #endregion
    }
}
