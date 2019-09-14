// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class MoveMessageErrorPolicyTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;
        private readonly IBroker _broker;

        public MoveMessageErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddBus();

            services.AddBroker<TestBroker>(options => { });

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            _broker = serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();
        }

        [Fact]
        public void HandleError_InboundMessage_MessageMoved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);
            var message = new InboundMessage(
                new byte[1],
                null,
                null, TestEndpoint.Default, true);

            message.Content = "hey oh!";

            policy.HandleError(new[]
            {
                message
            }, new Exception("test"));
            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            producer.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public void HandleError_InboundMessage_MessagePreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            var content = new TestEventOne { Content = "hey oh!" };
            var headers = new MessageHeaderCollection();
            var rawContent = TestEndpoint.Default.Serializer.Serialize(content, headers);
            var message = new InboundMessage(rawContent, headers, null, TestEndpoint.Default, true);
            message.Content = content;
            message.Headers.Add("key1", "value1");
            message.Headers.Add("key2", "value2");
            policy.HandleError(new[]{message}, new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            var producedMessage = producer.ProducedMessages.Last();
            var deserializedMessage = producedMessage.Endpoint.Serializer.Deserialize(producedMessage.Message, producedMessage.Headers);
            deserializedMessage.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public void HandleError_NotDeserializedInboundMessage_MessagePreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            var message = new InboundMessage(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null, TestEndpoint.Default, true);
            message.Content = null;
            message.Headers.Add("key1", "value1");
            message.Headers.Add("key2", "value2");

            policy.HandleError(new[] { message }, new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);
            var producedMessage = producer.ProducedMessages.Last();

            producedMessage.Message.Should().Equal(producedMessage.Message);
        }

        [Fact]
        public void HandleError_InboundMessage_HeadersPreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            var message = new InboundMessage(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null, TestEndpoint.Default, true);
            message.Content = "hey oh!";
            message.Headers.Add("key1", "value1");
            message.Headers.Add("key2", "value2");
            policy.HandleError(new[] { message }, new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            producer.ProducedMessages.Last().Headers.Should().Contain(message.Headers);
        }

        [Fact]
        public void Transform_InboundMessage_MessageTranslated()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default)
                .Transform((msg, ex) => new TestEventTwo());

            policy.HandleError(new[]
            {
                new InboundMessage(
                    Encoding.UTF8.GetBytes("hey oh!"),
                    new[] { new MessageHeader(MessageHeader.MessageTypeKey, typeof(System.String).AssemblyQualifiedName) },
                    null, TestEndpoint.Default, true)
            }, new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);
            var producedMessage = producer.Endpoint.Serializer.Deserialize(producer.ProducedMessages[0].Message, producer.ProducedMessages[0].Headers);
            producedMessage.Should().BeOfType<TestEventTwo>();
        }

        [Fact]
        public void Transform_InboundMessage_HeadersProperlyModified()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default)
                .Transform((msg, ex) => new TestEventTwo(), (headers, ex) =>
                {
                    headers.Add("error", ex.GetType().Name);
                    return headers;
                });

            var message = new InboundMessage(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null, TestEndpoint.Default, true);
            message.Headers.Add("key", "value");
            policy.HandleError(new[] { message }, new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);
            var newHeaders = producer.ProducedMessages[0].Headers;
            newHeaders.Count().Should().Be(4); // message-id, message-type, key, error
        }
    }
}