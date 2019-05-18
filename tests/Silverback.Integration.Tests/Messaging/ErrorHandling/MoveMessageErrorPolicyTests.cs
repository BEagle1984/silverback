// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
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

            var serviceProvider = services.BuildServiceProvider();

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            _broker = serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();
        }

        [Fact]
        public void TryHandleMessage_Failed_MessageMoved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            policy.HandleError(new FailedMessage(new TestEventOne()), new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            producer.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public void Transform_Failed_MessageTranslated()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default)
                .Transform((msg, ex) => new TestEventTwo());

            policy.HandleError(new FailedMessage(new TestEventOne()), new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);
            var producedMessage = producer.Endpoint.Serializer.Deserialize(producer.ProducedMessages[0].Message);
            producedMessage.Should().BeOfType<TestEventTwo>();
        }
    }
}