// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaProducerConfigTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(null, true)]
        [InlineData(false, false)]
        public void AreDeliveryReportsEnabled_CorrectlySet(bool? enableDeliveryReports, bool expected)
        {
            var config = new KafkaProducerConfig
            {
                EnableDeliveryReports = enableDeliveryReports
            };

            config.AreDeliveryReportsEnabled.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, "all", true)]
        [InlineData(true, "some,fields,and,status", true)]
        [InlineData(null, "all", true)]
        [InlineData(null, "some,fields,and,status", true)]
        [InlineData(true, "some,fields", false)]
        [InlineData(false, "all", false)]
        [InlineData(false, "some,fields,and,status", false)]
        public void ArePersistenceStatusReportsEnabled_CorrectlySet(
            bool? enableDeliveryReports,
            string deliveryReportFields,
            bool expected)
        {
            var config = new KafkaProducerConfig
            {
                EnableDeliveryReports = enableDeliveryReports,
                DeliveryReportFields = deliveryReportFields
            };

            config.ArePersistenceStatusReportsEnabled.Should().Be(expected);
        }

        [Fact]
        public void Validate_ValidConfiguration_NoExceptionThrown()
        {
            var config = GetValidConfig();

            Action act = () => config.Validate();

            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_MissingBootstrapServers_ExceptionThrown()
        {
            var config = GetValidConfig();

            config.BootstrapServers = string.Empty;

            Action act = () => config.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Theory]
        [InlineData(true, "some,fields")]
        [InlineData(null, "some,fields")]
        [InlineData(false, "all")]
        [InlineData(false, "status")]
        public void Validate_ThrowIfNotAcknowledgedWithoutStatusReport_ExceptionThrown(
            bool? enableDeliveryReports,
            string deliveryReportFields)
        {
            var config = GetValidConfig();

            config.EnableDeliveryReports = enableDeliveryReports;
            config.DeliveryReportFields = deliveryReportFields;

            Action act = () => config.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Theory]
        [InlineData(true, "")]
        [InlineData(true, "all")]
        [InlineData(true, "status")]
        [InlineData(null, "")]
        [InlineData(null, "all")]
        [InlineData(null, "status")]
        public void Validate_ThrowIfNotAcknowledgedWithStatusReport_NoExceptionThrown(
            bool? enableDeliveryReports,
            string deliveryReportFields)
        {
            var config = GetValidConfig();

            config.EnableDeliveryReports = enableDeliveryReports;
            config.DeliveryReportFields = deliveryReportFields;

            Action act = () => config.Validate();

            act.Should().NotThrow();
        }

        private static KafkaProducerConfig GetValidConfig() => new KafkaProducerConfig
        {
            BootstrapServers = "test-server"
        };
    }
}
