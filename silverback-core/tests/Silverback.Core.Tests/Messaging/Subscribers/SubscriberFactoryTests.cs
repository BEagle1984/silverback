using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class SubscriberFactoryTests
    {
        [Test]
        public async Task BasicTest()
        {
            var service = new TestService();
            var subscriberFactory = new SubscriberFactory<IService>(new GenericTypeFactory(t => service));
            subscriberFactory.Init(new BusBuilder().Build());

            subscriberFactory.OnNext(new TransactionCommitEvent());
            await subscriberFactory.OnNextAsync(new TransactionRollbackEvent());
            await subscriberFactory.OnNextAsync(new TransactionCommitEvent());
            subscriberFactory.OnNext(new TransactionRollbackEvent());

            Assert.That(service.Handled, Is.EqualTo(4));
        }

        [Test]
        public async Task MultipleSubscribersTest()
        {
            var service1 = new ServiceOne();
            var service2 = new ServiceTwo();
            var subscriberFactory = new SubscriberFactory<IService>(
                new GenericTypeFactory(t => new object[] { service1, service2 }));
            subscriberFactory.Init(new BusBuilder().Build());

            subscriberFactory.OnNext(new TestCommandOne());
            await subscriberFactory.OnNextAsync(new TestCommandTwo());
            subscriberFactory.OnNext(new TransactionCommitEvent());
            await subscriberFactory.OnNextAsync(new TransactionRollbackEvent());

            Assert.That(service1.Handled, Is.EqualTo(4));
            Assert.That(service2.Handled, Is.EqualTo(4));
        }
    }
}
