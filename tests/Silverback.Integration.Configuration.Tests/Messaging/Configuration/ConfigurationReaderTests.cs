// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Configuration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Configuration.Messaging.Configuration
{
    public class ConfigurationReaderTests
    {
        private readonly IServiceProvider _serviceProvider;

        public ConfigurationReaderTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton(Substitute.For<IBrokerCollection>());
            services.AddScoped(s => Substitute.For<IPublisher>());

            _serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });
        }

        [Fact]
        public void Read_SimplestInbound_EndpointAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest"));

            reader.Inbound.Count.Should().Be(2);
            reader.Inbound.First().Endpoint.Should().NotBeNull();
        }

        [Fact]
        public void Read_SimplestInbound_CorrectEndpointType()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest"));

            var endpoint = reader.Inbound.First().Endpoint;
            endpoint.Should().BeOfType<KafkaConsumerEndpoint>();
        }

        [Fact]
        public void Read_SimplestInbound_EndpointNameSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest"));

            var endpoint = reader.Inbound.Skip(1).First().Endpoint;
            endpoint.Name.Should().Be("inbound-endpoint2");
        }

        [Fact]
        public void Read_SimplestInbound_DefaultSerializer()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.simplest"));

            var endpoint = reader.Inbound.First().Endpoint;
            endpoint.Serializer.Should().NotBeNull();
        }

        [Fact]
        public void Read_CompleteInbound_DefaultSerializerPropertiesSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var serializer = (JsonMessageSerializer)reader.Inbound.Skip(1).First().Endpoint.Serializer;
            serializer.Encoding.Should().Be(MessageEncoding.Unicode);
            serializer.Settings.Formatting.Should().Be(Formatting.Indented);
        }

        [Fact]
        public void Read_CompleteInbound_SettingsSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var settings = reader.Inbound.First().Settings;
            settings.Should().NotBeNull();
        }

        [Fact]
        public void Read_CompleteInbound_SettingsBatchSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var batchSettings = reader.Inbound.First().Settings.Batch;
            batchSettings.Should().NotBeNull();
        }

        [Fact]
        public void Read_CompleteInbound_SettingsConsumersSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var settings = reader.Inbound.First().Settings;
            settings.Consumers.Should().Be(3);
        }

        [Fact]
        public void Read_CompleteInbound_SettingsBatchPropertiesSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var batchSettings = reader.Inbound.First().Settings.Batch;
            batchSettings.Size.Should().Be(5);
            batchSettings.MaxWaitTime.Should().Be(TimeSpan.FromMilliseconds(2500));
        }

        [Fact]
        public void Read_CompleteInbound_EndpointSubPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var endpoint = (KafkaConsumerEndpoint)reader.Inbound.First().Endpoint;

            endpoint.Configuration.AutoOffsetReset.Should().Be(AutoOffsetReset.Earliest);
        }

        [Fact]
        public void Read_CompleteInbound_EndpointNameSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var endpoint = (KafkaConsumerEndpoint)reader.Inbound.First().Endpoint;

            endpoint.Names.Should().BeEquivalentTo("inbound-endpoint1");
        }

        [Fact]
        public void Read_CompleteInbound_EndpointNamesSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var endpoint = (KafkaConsumerEndpoint)reader.Inbound.Skip(1).First().Endpoint;

            endpoint.Names.Should().BeEquivalentTo("inbound-endpoint1", "inbound-endpoint2");
        }

        [Fact]
        public void Read_CompleteInbound_CustomSerializerSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var endpoint = reader.Inbound.First().Endpoint;
            endpoint.Serializer.Should().BeOfType<FakeSerializer>();
        }

        [Fact]
        public void Read_CompleteInbound_CustomSerializerPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var serializer = (FakeSerializer)reader.Inbound.First().Endpoint.Serializer;
            serializer.Settings.Mode.Should().Be(4);
        }

        [Fact]
        public void Read_CompleteInbound_ErrorPoliciesAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            reader.Inbound.First().ErrorPolicies.Should().HaveCount(2);
        }

        [Fact]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
        public void Read_CompleteInbound_ErrorPolicyMaxFailedAttemptsSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            policy.CanHandle(
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                    null,
                    new KafkaConsumerEndpoint("test"),
                    "test"),
                new ArgumentException()).Should().BeTrue();
            policy.CanHandle(
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "6") },
                    null,
                    new KafkaConsumerEndpoint("test"),
                    "test"),
                new ArgumentException()).Should().BeFalse();
        }

        [Fact]
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public void Read_CompleteInbound_ErrorPolicyConstructorParameterSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var policy = (RetryErrorPolicy)reader.Inbound.First().ErrorPolicies.First();
            ((TimeSpan)policy.GetType().GetField("_delayIncrement", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(policy)!).Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
        public void Read_CompleteInbound_ErrorPolicyApplyToSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            policy.CanHandle(
                new InboundEnvelope(new byte[1], null, null, new KafkaConsumerEndpoint("test"), "test"),
                new ArgumentException()).Should().BeTrue();
            policy.CanHandle(
                new InboundEnvelope(new byte[1], null, null, new KafkaConsumerEndpoint("test"), "test"),
                new InvalidOperationException()).Should().BeTrue();
            policy.CanHandle(
                new InboundEnvelope(new byte[1], null, null, new KafkaConsumerEndpoint("test"), "test"),
                new FormatException()).Should().BeFalse();
        }

        [Fact]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
        public void Read_CompleteInbound_ErrorPolicyExcludeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            var policy = reader.Inbound.First().ErrorPolicies.First();
            policy.CanHandle(
                new InboundEnvelope(new byte[1], null, null, new KafkaConsumerEndpoint("test"), "test"),
                new ArgumentException()).Should().BeTrue();
            policy.CanHandle(
                new InboundEnvelope(new byte[1], null, null, new KafkaConsumerEndpoint("test"), "test"),
                new ArgumentNullException()).Should().BeFalse();
        }

        [Fact]
        public void Read_CompleteInbound_ConnectorTypeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("inbound.complete"));

            reader.Inbound.Skip(1).First().ConnectorType.Should().Be(typeof(LoggedInboundConnector));
        }

        [Fact]
        public void Read_SimplestOutbound_EndpointAdded()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            reader.Outbound.Should().HaveCount(1);
            reader.Outbound.First().Endpoint.Should().NotBeNull();
        }

        [Fact]
        public void Read_SimplestOutbound_CorrectEndpointType()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            var endpoint = reader.Outbound.First().Endpoint;
            endpoint.Should().BeOfType<KafkaProducerEndpoint>();
        }

        [Fact]
        public void Read_SimplestOutbound_EndpointNameSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            var endpoint = reader.Outbound.First().Endpoint;
            endpoint.Name.Should().Be("outbound-endpoint1");
        }

        [Fact]
        public void Read_SimplestOutbound_DefaultSerializer()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            var endpoint = reader.Outbound.First().Endpoint;
            endpoint.Serializer.Should().NotBeNull();
        }

        [Fact]
        public void Read_SimpleOutbound_DefaultMessageType()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            reader.Outbound.First().MessageType.Should().Be(typeof(object));
        }

        [Fact]
        public void Read_CompleteOutbound_DefaultSerializerPropertiesSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            var serializer = (JsonMessageSerializer)reader.Outbound.Skip(1).First().Endpoint.Serializer;
            serializer.Encoding.Should().Be(MessageEncoding.Unicode);
            serializer.Settings.Formatting.Should().Be(Formatting.Indented);
        }

        [Fact]
        public void Read_CompleteOutbound_EndpointSubPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            var endpoint = (KafkaProducerEndpoint)reader.Outbound.First().Endpoint;
            endpoint.Configuration.EnableBackgroundPoll.Should().BeFalse();
        }

        [Fact]
        public void Read_CompleteOutbound_CustomSerializerSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            var endpoint = reader.Outbound.First().Endpoint;
            endpoint.Serializer.Should().BeOfType<FakeSerializer>();
        }

        [Fact]
        public void Read_CompleteOutbound_CustomSerializerPropertySet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            var serializer = (FakeSerializer)reader.Outbound.First().Endpoint.Serializer;
            serializer.Settings.Mode.Should().Be(4);
        }

        [Fact]
        public void Read_CompleteOutbound_ConnectorTypeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            reader.Outbound.Skip(1).First().ConnectorType.Should().Be(typeof(DeferredOutboundConnector));
        }

        [Fact]
        public void Read_CompleteOutbound_MessageTypeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            reader.Outbound.First().MessageType.Should().Be(typeof(IIntegrationEvent));
        }

        [Fact]
        public void Read_SimplestOutbound_DefaultChunkSize()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"));

            var endpoint = (KafkaProducerEndpoint)reader.Outbound.First().Endpoint;
            endpoint.Chunk.Size.Should().Be(int.MaxValue);
        }

        [Fact]
        public void Read_SimplestOutbound_ChunkSizeSet()
        {
            var reader =
                new ConfigurationReader(_serviceProvider)
                    .Read(ConfigFileHelper.GetConfigSection("outbound.complete"));

            var endpoint = (KafkaProducerEndpoint)reader.Outbound.First().Endpoint;
            endpoint.Chunk.Size.Should().Be(100000);
        }

        // TODO: Need to rewrite them with a different approach since AddInbound and similar are now extension methods

        // [Fact]
        // public void ReadAndApply_SimpleInbound_AddInboundCalled()
        // {
        //     new ConfigurationReader(_serviceProvider)
        //         .Read(ConfigFileHelper.GetConfigSection("inbound.simplest"))
        //         .Apply(_builder);
        //
        //     _builder.ReceivedWithAnyArgs(2).AddInbound(null, null, null, null);
        // }
        //
        // [Fact]
        // public void ReadAndApply_CompleteInbound_AddInboundWithCorrectTypes()
        // {
        //     new ConfigurationReader(_serviceProvider)
        //         .Read(ConfigFileHelper.GetConfigSection("inbound.complete"))
        //         .Apply(_builder);
        //
        //     // TODO: Should check policies in first call
        //     _builder.Received(1).AddInbound(
        //         Arg.Any<KafkaConsumerEndpoint>(),
        //         null,
        //         Arg.Any<Func<IErrorPolicyBuilder, IErrorPolicy>>(),
        //         Arg.Any<InboundConnectorSettings>());
        //     _builder.Received(1).AddInbound(
        //         Arg.Any<KafkaConsumerEndpoint>(),
        //         typeof(LoggedInboundConnector),
        //         Arg.Is<Func<IErrorPolicyBuilder, IErrorPolicy>>(f => f.Invoke(null) == null));
        // }
        //
        // [Fact]
        // public void ReadAndApply_SimpleOutbound_OutboundAdded()
        // {
        //     new ConfigurationReader(_serviceProvider)
        //         .Read(ConfigFileHelper.GetConfigSection("outbound.simplest"))
        //         .Apply(_builder);
        //
        //     _builder.ReceivedWithAnyArgs(1).AddOutbound(null, (IEnumerable<IProducerEndpoint>) null, null);
        // }
        //
        // [Fact]
        // public void ReadAndApply_CompleteOutbound_AddOutboundWithCorrectTypes()
        // {
        //     new ConfigurationReader(_serviceProvider)
        //         .Read(ConfigFileHelper.GetConfigSection("outbound.complete"))
        //         .Apply(_builder);
        //
        //     _builder.Received(1).AddOutbound(
        //         typeof(IIntegrationEvent),
        //         Arg.Any<IProducerEndpoint[]>(),
        //         null);
        //     _builder.Received(1).AddOutbound(
        //         typeof(IIntegrationCommand),
        //         Arg.Any<IProducerEndpoint[]>(),
        //         typeof(DeferredOutboundConnector));
        // }
    }
}
