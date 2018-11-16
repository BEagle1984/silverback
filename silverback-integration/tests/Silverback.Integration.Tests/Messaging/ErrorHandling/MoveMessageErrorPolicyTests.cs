using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class MoveMessageErrorPolicyTests
    {
        private ErrorPolicyBuilder _errorPolicyBuilder;
        private IBroker _broker;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddSingleton<IPublisher, Publisher>();

            services.AddBroker<TestBroker>(options => { });

            var serviceProvider = services.BuildServiceProvider();

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            _broker = serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();
        }

        [Test]
        public void TryHandleMessage_Successful_NotMoved()
        {
            var executed = false;
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => executed = true);

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            Assert.That(executed, Is.True);
            Assert.That(producer.SentMessages.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryHandleMessage_Failed_MessageMoved()
        {
            var policy = _errorPolicyBuilder.Move(TestEndpoint.Default);

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Default);

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
        }
    }
}