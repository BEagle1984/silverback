// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ProducerTests : KafkaTestFixture
    {
        public ProducerTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Produce_Message_Produced()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.Produce(message);
            producer.Produce(message);
            producer.Produce(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            }
        }

        [Fact]
        public async Task Produce_MessageWithHeaders_Produced()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task Produce_Envelope_Produced()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll()!;

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));
            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));
            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task Produce_MessageUsingCallbacks_Produced()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.Produce(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task Produce_EnvelopeUsingCallbacks_Produced()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.Produce(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduce_ByteArray_Produced()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.RawProduce(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.RawProduce(
                DefaultTopicName,
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.RawProduce(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduce_Stream_Produced()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.RawProduce(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.RawProduce(
                DefaultTopicName,
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            producer.RawProduce(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduce_ByteArrayUsingCallbacks_Produced()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.RawProduce(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.RawProduce(
                producer.Endpoint.Name,
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.RawProduce(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduce_StreamUsingCallbacks_Produced()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll()!;

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            producer.RawProduce(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.RawProduce(
                producer.Endpoint.Name,
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            producer.RawProduce(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task ProduceAsync_Message_Produced()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(message);
            await producer.ProduceAsync(message);
            await producer.ProduceAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            }
        }

        [Fact]
        public async Task ProduceAsync_MessageWithHeaders_ProducedAndConsumed()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task ProduceAsync_Envelope_ProducedAndConsumed()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll()!;

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));
            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));
            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task ProduceAsync_MessageUsingCallbacks_ProducedAndConsumed()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.ProduceAsync(
                message,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task ProduceAsync_EnvelopeUsingCallbacks_ProducedAndConsumed()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.ProduceAsync(
                EnvelopeFactory.Create(
                    message,
                    new MessageHeaderCollection
                    {
                        { "one", "1" }, { "two", "2" }
                    },
                    producer.Endpoint),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduceAsync_ByteArray_ProducedAndConsumed()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.RawProduceAsync(
                DefaultTopicName,
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduceAsync_Stream_ProducedAndConsumed()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.RawProduceAsync(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.RawProduceAsync(
                DefaultTopicName,
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });
            await producer.RawProduceAsync(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduceAsync_ByteArrayUsingCallbacks_ProducedAndConsumed()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.RawProduceAsync(
                producer.Endpoint.Name,
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduceAsync_StreamUsingCallbacks_ProducedAndConsumed()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll()!;

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.RawProduceAsync(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.RawProduceAsync(
                producer.Endpoint.Name,
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
            await producer.RawProduceAsync(
                new MemoryStream(rawMessage),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 3);

            produced.Should().Be(3);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (var envelope in Helper.Spy.InboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("one", "1"));
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader("two", "2"));
            }
        }

        [Fact]
        public async Task RawProduce_WithMessageIdHeader_KafkaKeySet()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers = new MessageHeaderCollection()
            {
                { DefaultMessageHeaders.MessageId, "42-42-42" }
            };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    headers,
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            for (var i = 0; i < 5; i++)
            {
                await producer.RawProduceAsync(
                    rawMessage,
                    headers,
                    _ => Interlocked.Increment(ref produced),
                    _ => Interlocked.Increment(ref errors));
            }

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 5);

            produced.Should().Be(5);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "42-42-42"));
            }
        }

        [Fact]
        public async Task RawProduce_WithKafkaKeyHeader_KafkaKeySet()
        {
            int produced = 0;
            int errors = 0;

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers = new MessageHeaderCollection()
            {
                { KafkaMessageHeaders.KafkaMessageKey, "42-42-42" }
            };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    headers,
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            for (var i = 0; i < 5; i++)
            {
                await producer.RawProduceAsync(
                    rawMessage,
                    headers,
                    _ => Interlocked.Increment(ref produced),
                    _ => Interlocked.Increment(ref errors));
            }

            produced.Should().BeLessThan(3);

            await AsyncTestingUtil.WaitAsync(() => produced == 5);

            produced.Should().Be(5);
            errors.Should().Be(0);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

            foreach (var envelope in Helper.Spy.RawInboundEnvelopes)
            {
                envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
                envelope.Headers.Should()
                    .ContainEquivalentOf(new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "42-42-42"));
            }
        }
    }
}
