// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaProducerConfigurationBuilder builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithBootstrapServers("one");
        KafkaProducerConfiguration configuration1 = builder.Build();

        builder.WithBootstrapServers("two");
        KafkaProducerConfiguration configuration2 = builder.Build();

        configuration1.BootstrapServers.ShouldBe("one");
        configuration2.BootstrapServers.ShouldBe("two");
    }

    [Fact]
    public void Produce_ShouldAddEndpoints()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(2);
        KafkaProducerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        KafkaStaticProducerEndpointResolver resolver1 = endpoint1.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        resolver1.TopicPartition.ShouldBe(new TopicPartition("topic1", Partition.Any));
        endpoint1.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        KafkaProducerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        KafkaStaticProducerEndpointResolver resolver2 = endpoint2.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        resolver2.TopicPartition.ShouldBe(new TopicPartition("topic2", Partition.Any));
        endpoint2.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldAddEndpointWithGenericMessageType_WhenNoTypeIsSpecified()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder.Produce(endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(1);
        KafkaProducerEndpointConfiguration endpoint = configuration.Endpoints.Single();
        endpoint.MessageType.ShouldBe(typeof(object));
        KafkaStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        resolver.TopicPartition.ShouldBe(new TopicPartition("topic1", Partition.Any));
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id1", endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(1);
        KafkaProducerEndpointConfiguration endpoint = configuration.Endpoints.First();
        KafkaStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        resolver.TopicPartition.ShouldBe(new TopicPartition("topic1", Partition.Any));
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").EnableChunking(42))
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").EnableChunking(1000));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(1);
        KafkaProducerEndpointConfiguration endpoint = configuration.Endpoints.First();
        KafkaStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        resolver.TopicPartition.ShouldBe(new TopicPartition("topic1", Partition.Any));
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        endpoint.Chunk!.Size.ShouldBe(42);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentMessageType()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(2);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id2", endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(2);
    }

    [Fact]
    public void ThrowIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.ThrowIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.ThrowIfNotAcknowledged.ShouldBe(true);
    }

    [Fact]
    public void IgnoreIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.IgnoreIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.ThrowIfNotAcknowledged.ShouldBe(false);
    }

    [Fact]
    public void DisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisposeOnException();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.DisposeOnException.ShouldBe(true);
    }

    [Fact]
    public void DisableDisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableDisposeOnException();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.DisposeOnException.ShouldBe(false);
    }

    [Fact]
    public void WithFlushTimeout_ShouldSetFlushTimeout()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.WithFlushTimeout(TimeSpan.FromHours(42));

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.FlushTimeout.ShouldBe(TimeSpan.FromHours(42));
    }

    [Fact]
    public void WithTransactionsInitTimeout_ShouldSetTransactionsInitTimeout()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.WithTransactionsInitTimeout(TimeSpan.FromHours(42));

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.TransactionsInitTimeout.ShouldBe(TimeSpan.FromHours(42));
    }

    [Fact]
    public void WithTransactionCommitTimeout_ShouldSetTransactionCommitTimeout()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.WithTransactionCommitTimeout(TimeSpan.FromHours(42));

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.TransactionCommitTimeout.ShouldBe(TimeSpan.FromHours(42));
    }

    [Fact]
    public void WithTransactionAbortTimeout_ShouldSetTransactionAbortTimeout()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.WithTransactionAbortTimeout(TimeSpan.FromHours(42));

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.TransactionAbortTimeout.ShouldBe(TimeSpan.FromHours(42));
    }

    [Fact]
    public void EnableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableDeliveryReports();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableDeliveryReports.ShouldBe(true);
    }

    [Fact]
    public void DisableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableDeliveryReports();
        builder.IgnoreIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableDeliveryReports.ShouldBe(false);
    }

    [Fact]
    public void EnableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableIdempotence();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableIdempotence.ShouldBe(true);
    }

    [Fact]
    public void DisableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableIdempotence();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableIdempotence.ShouldBe(false);
    }

    [Fact]
    public void EnableGaplessGuarantee_ShouldSetEnableGaplessGuarantee()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableGaplessGuarantee();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableGaplessGuarantee.ShouldBe(true);
    }

    [Fact]
    public void DisableGaplessGuarantee_ShouldSetEnableGaplessGuarantee()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableGaplessGuarantee();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.EnableGaplessGuarantee.ShouldBe(false);
    }

    [Fact]
    public void EnableTransactions_ShouldSetTransactionalId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableTransactions("transactional-id");

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.TransactionalId.ShouldBe("transactional-id");
    }

    [Fact]
    public void DisableTransactions_ShouldSetTransactionalId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableTransactions("transactional-id");
        builder.DisableTransactions();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.ShouldNotBeNull();
        configuration.TransactionalId.ShouldBeNull();
    }

    private static KafkaProducerConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint() =>
        GetBuilderWithValidConfiguration().Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

    private static KafkaProducerConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://test");
}
