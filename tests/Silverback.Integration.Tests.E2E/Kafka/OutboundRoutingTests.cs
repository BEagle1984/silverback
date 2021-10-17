// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class OutboundRoutingTests : KafkaTestFixture
    {
        public OutboundRoutingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task OutboundRouting_AnyPartition_MessagesRoutedToRandomPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });
            await publisher.PublishAsync(new TestEventOne { Content = "4" });
            await publisher.PublishAsync(new TestEventOne { Content = "5" });

            DefaultTopic.Partitions
                .Where(partition => partition.Messages.Count > 0)
                .Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task OutboundRouting_SpecificPartition_MessagesRoutedToSpecifiedPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName, 3))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });
            await publisher.PublishAsync(new TestEventOne { Content = "4" });
            await publisher.PublishAsync(new TestEventOne { Content = "5" });

            DefaultTopic.Partitions[0].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[1].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[3].Messages.Count.Should().Be(5);
            DefaultTopic.Partitions[4].Messages.Count.Should().Be(0);
        }

        [Fact]
        public async Task OutboundRouting_AnyPartitionWithPartitionKey_MessagesRoutedToPredictablePartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

            DefaultTopic.Partitions[0].Messages.Count.Should().Be(2);
            DefaultTopic.Partitions[1].Messages.Count.Should().Be(1);
            DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[3].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[4].Messages.Count.Should().Be(2);
        }

        [Fact]
        public async Task OutboundRouting_AnyPartitionWithNullPartitionKey_MessagesRoutedToRandomPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "1" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "2" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "3" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "4" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "5" });

            DefaultTopic.Partitions
                .Where(partition => partition.Messages.Count > 0)
                .Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task
            OutboundRouting_AnyPartitionWithNullOrEmptyPartitionKey_MessagesRoutedToRandomPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "1" });
            await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "2" });
            await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "3" });
            await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "4" });
            await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "5" });

            DefaultTopic.Partitions
                .Where(partition => partition.Messages.Count > 0)
                .Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task
            OutboundRouting_SpecificPartitionWithPartitionKey_MessagesRoutedToSpecifiedPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName, 3))))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

            DefaultTopic.Partitions[0].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[1].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
            DefaultTopic.Partitions[3].Messages.Count.Should().Be(5);
            DefaultTopic.Partitions[4].Messages.Count.Should().Be(0);
        }

        [Fact]
        public async Task DynamicRouting_NameFunction_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo<TestEventOne>(
                                            envelope =>
                                            {
                                                switch (envelope.Message!.Content)
                                                {
                                                    case "1":
                                                        return "topic1";
                                                    case "2":
                                                        return "topic2";
                                                    case "3":
                                                        return "topic3";
                                                    default:
                                                        throw new InvalidOperationException();
                                                }
                                            }))))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(2);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task DynamicRouting_NameAndPartitionFunctions_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo<TestEventOne>(
                                            envelope =>
                                            {
                                                switch (envelope.Message!.Content)
                                                {
                                                    case "1":
                                                        return "topic1";
                                                    case "2":
                                                        return "topic2";
                                                    case "3":
                                                        return "topic3";
                                                    default:
                                                        throw new InvalidOperationException();
                                                }
                                            },
                                            envelope =>
                                            {
                                                switch (envelope.Message!.Content)
                                                {
                                                    case "1":
                                                        return 2;
                                                    case "2":
                                                        return 3;
                                                    default:
                                                        return Partition.Any;
                                                }
                                            }))))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var partition1 = topic1.Partitions[2];
            var partition2 = topic2.Partitions[3];

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);
            partition1.Messages.Count.Should().Be(1);
            partition2.Messages.Count.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(2);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(2);
            partition1.Messages.Count.Should().Be(2);
            partition2.Messages.Count.Should().Be(1);
        }

        [Fact]
        public async Task DynamicRouting_NameFormat_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo<TestEventOne>(
                                            "topic{0}",
                                            envelope =>
                                            {
                                                switch (envelope.Message!.Content)
                                                {
                                                    case "1":
                                                        return new[] { "1" };
                                                    case "2":
                                                        return new[] { "2" };
                                                    case "3":
                                                        return new[] { "3" };
                                                    default:
                                                        throw new InvalidOperationException();
                                                }
                                            }))))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(2);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task DynamicRouting_CustomNameResolver_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSingleton<TestEndpointNameResolver>()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .UseEndpointNameResolver<TestEndpointNameResolver>())))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var partition1 = topic1.Partitions[2];
            var partition2 = topic2.Partitions[3];

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);
            partition1.Messages.Count.Should().Be(1);
            partition2.Messages.Count.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(2);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(2);
            partition1.Messages.Count.Should().Be(2);
            partition2.Messages.Count.Should().Be(1);
        }

        [Fact]
        public async Task DynamicRouting_CustomOutboundRouter_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddSingletonOutboundRouter<TestOutboundRouter>()
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne, TestOutboundRouter>()))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1,2,3" });
            await publisher.PublishAsync(new TestEventOne { Content = "2,1" });

            topic1.MessagesCount.Should().Be(3);
            topic2.MessagesCount.Should().Be(3);
            topic3.MessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task DynamicRouting_GenericSingleEndpointRouter_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary["one"],
                                        "two" => endpointsDictionary["two"],
                                        _ => endpointsDictionary["three"]
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) =>
                                        ((TestEventTwo)message).Content switch
                                        {
                                            "one" => endpointsDictionary["one"],
                                            "two" => endpointsDictionary["two"],
                                            _ => endpointsDictionary["three"]
                                        },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    })))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var topicA = Helper.GetTopic("topicA");
            var topicB = Helper.GetTopic("topicB");
            var topicC = Helper.GetTopic("topicC");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(0);
            topicA.MessagesCount.Should().Be(0);
            topicB.MessagesCount.Should().Be(0);
            topicC.MessagesCount.Should().Be(0);

            await publisher.PublishAsync(new TestEventTwo { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "two" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(0);
            topicA.MessagesCount.Should().Be(1);
            topicB.MessagesCount.Should().Be(1);
            topicC.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task DynamicRouting_GenericSingleEndpointRouter_MessagesRoutedToPartition()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary["one"],
                                        "two" => endpointsDictionary["two"],
                                        _ => endpointsDictionary["three"]
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1", 1) },
                                        { "two", endpoint => endpoint.ProduceTo("topic1", 2) },
                                        { "three", endpoint => endpoint.ProduceTo("topic1", 3) }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) =>
                                        ((TestEventTwo)message).Content switch
                                        {
                                            "one" => endpointsDictionary["one"],
                                            "two" => endpointsDictionary["two"],
                                            _ => endpointsDictionary["three"]
                                        },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA", 1) },
                                        { "two", endpoint => endpoint.ProduceTo("topicA", 2) },
                                        { "three", endpoint => endpoint.ProduceTo("topicA", 3) }
                                    })))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topicA = Helper.GetTopic("topicA");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "one" });

            topic1.Partitions[0].Messages.Count.Should().Be(0);
            topic1.Partitions[1].Messages.Count.Should().Be(1);
            topic1.Partitions[2].Messages.Count.Should().Be(0);
            topic1.Partitions[3].Messages.Count.Should().Be(0);
            topic1.Partitions[4].Messages.Count.Should().Be(0);
            topicA.Partitions[0].Messages.Count.Should().Be(0);
            topicA.Partitions[1].Messages.Count.Should().Be(0);
            topicA.Partitions[2].Messages.Count.Should().Be(0);
            topicA.Partitions[3].Messages.Count.Should().Be(0);
            topicA.Partitions[4].Messages.Count.Should().Be(0);

            await publisher.PublishAsync(new TestEventOne { Content = "two" });

            topic1.Partitions[0].Messages.Count.Should().Be(0);
            topic1.Partitions[1].Messages.Count.Should().Be(1);
            topic1.Partitions[2].Messages.Count.Should().Be(1);
            topic1.Partitions[3].Messages.Count.Should().Be(0);
            topic1.Partitions[4].Messages.Count.Should().Be(0);
            topicA.Partitions[0].Messages.Count.Should().Be(0);
            topicA.Partitions[1].Messages.Count.Should().Be(0);
            topicA.Partitions[2].Messages.Count.Should().Be(0);
            topicA.Partitions[3].Messages.Count.Should().Be(0);
            topicA.Partitions[4].Messages.Count.Should().Be(0);

            await publisher.PublishAsync(new TestEventTwo { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "two" });

            topic1.Partitions[0].Messages.Count.Should().Be(0);
            topic1.Partitions[1].Messages.Count.Should().Be(1);
            topic1.Partitions[2].Messages.Count.Should().Be(1);
            topic1.Partitions[3].Messages.Count.Should().Be(0);
            topic1.Partitions[4].Messages.Count.Should().Be(0);
            topicA.Partitions[0].Messages.Count.Should().Be(0);
            topicA.Partitions[1].Messages.Count.Should().Be(1);
            topicA.Partitions[2].Messages.Count.Should().Be(1);
            topicA.Partitions[3].Messages.Count.Should().Be(0);
            topicA.Partitions[4].Messages.Count.Should().Be(0);
        }

        [Fact]
        public async Task DynamicRouting_WithProducerPreloading_ProducersPreloaded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary.Values.Take(1),
                                        "two-three" => endpointsDictionary.Values.Skip(1),
                                        _ => throw new ArgumentOutOfRangeException()
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) =>
                                        ((TestEventTwo)message).Content switch
                                        {
                                            "one" => endpointsDictionary.Values.Take(1),
                                            "two-three" => endpointsDictionary.Values.Skip(1),
                                            _ => throw new ArgumentOutOfRangeException()
                                        },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    })))
                .Run();

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");
            var topic3 = Helper.GetTopic("topic3");
            var topicA = Helper.GetTopic("topicA");
            var topicB = Helper.GetTopic("topicB");
            var topicC = Helper.GetTopic("topicC");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two-three" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);
            topicA.MessagesCount.Should().Be(0);
            topicB.MessagesCount.Should().Be(0);
            topicC.MessagesCount.Should().Be(0);

            await publisher.PublishAsync(new TestEventTwo { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "two-three" });

            topic1.MessagesCount.Should().Be(1);
            topic2.MessagesCount.Should().Be(1);
            topic3.MessagesCount.Should().Be(1);
            topicA.MessagesCount.Should().Be(1);
            topicB.MessagesCount.Should().Be(1);
            topicC.MessagesCount.Should().Be(1);
        }

        [Fact]
        public void DynamicRouting_WithoutProducerPreloading_ProducersLoadedAtFirstProduce()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary.Values.Take(1),
                                        "two-three" => endpointsDictionary.Values.Skip(1),
                                        _ => throw new ArgumentOutOfRangeException()
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) =>
                                        ((TestEventTwo)message).Content switch
                                        {
                                            "one" => endpointsDictionary.Values.Take(1),
                                            "two-three" => endpointsDictionary.Values.Skip(1),
                                            _ => throw new ArgumentOutOfRangeException()
                                        },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    }))
                        .AddIntegrationSpy())
                .Run();

            Helper.Broker.Producers.Count.Should().Be(6);
            Helper.Broker.Producers.ForEach(producer => producer.IsConnected.Should().BeTrue());
            Helper.Spy.OutboundEnvelopes.Should().BeEmpty();
        }

        [Fact]
        public async Task DynamicRouting_GenericMultipleEndpointRouter_ProducersPreloaded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary.Values.Take(1),
                                        "two-three" => endpointsDictionary.Values.Skip(1),
                                        _ => throw new ArgumentOutOfRangeException()
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    },
                                    false)
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) =>
                                        ((TestEventTwo)message).Content switch
                                        {
                                            "one" => endpointsDictionary.Values.Take(1),
                                            "two-three" => endpointsDictionary.Values.Skip(1),
                                            _ => throw new ArgumentOutOfRangeException()
                                        },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    },
                                    false))
                        .AddIntegrationSpy())
                .Run();

            Helper.Broker.Producers.Count.Should().Be(0);

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "one" });

            Helper.Broker.Producers.Count.Should().Be(2);
            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class TestEndpointNameResolver : KafkaProducerEndpointNameResolver<TestEventOne>
        {
            protected override string GetName(IOutboundEnvelope<TestEventOne> envelope)
            {
                switch (envelope.Message!.Content)
                {
                    case "1":
                        return "topic1";
                    case "2":
                        return "topic2";
                    case "3":
                        return "topic3";
                    default:
                        throw new InvalidOperationException();
                }
            }

            protected override int? GetPartition(IOutboundEnvelope<TestEventOne> envelope)
            {
                switch (envelope.Message!.Content)
                {
                    case "1":
                        return 2;
                    case "2":
                        return 3;
                    default:
                        return null;
                }
            }
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class TestOutboundRouter : OutboundRouter<TestEventOne>
        {
            private readonly IReadOnlyList<KafkaProducerEndpoint> _endpoints = new[]
            {
                new KafkaProducerEndpoint("topic1")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                },
                new KafkaProducerEndpoint("topic2")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                },
                new KafkaProducerEndpoint("topic3")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                }
            };

            public override IEnumerable<IProducerEndpoint> Endpoints => _endpoints;

            public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
                TestEventOne message,
                MessageHeaderCollection headers) =>
                message.Content?.Split(',')
                    .Select(int.Parse)
                    .Select(index => _endpoints[index - 1]) ??
                Enumerable.Empty<IProducerEndpoint>();
        }
    }
}
