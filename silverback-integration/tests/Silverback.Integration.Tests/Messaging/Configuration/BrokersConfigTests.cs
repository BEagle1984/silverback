using System.Linq;
using NUnit.Framework;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class BrokersConfigTests
    {
        private BrokersConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new BrokersConfig();
        }

        [Test]
        public void AddTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2"))
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Count, Is.EqualTo(3));
        }

        [Test]
        public void DefaultTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2"))
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<FakeBroker>());
            Assert.That(((FakeBroker) _config.Default).ServerName == "server1");
        }

        [Test]
        public void AddDefaultTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2").AsDefault())
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<FakeBroker>());
            Assert.That(((FakeBroker)_config.Default).ServerName == "server2");
        }

        [Test]
        public void AddNoNameTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<FakeBroker>());
            Assert.That(((FakeBroker)_config.Default).ServerName == "server1");
        }

        [Test]
        public void AddDuplicatedNameTest()
        {
            Assert.That(
                () => _config
                    .Add<FakeBroker>(c => c.UseServer("server1").WithName("duplicate"))
                    .Add<FakeBroker>(c => c.UseServer("server2").WithName("duplicate")),
                Throws.InvalidOperationException);
        }


        [Test]
        public void AddDuplicatedDefaultTest()
        {
            Assert.That(
                () => _config
                    .Add<FakeBroker>(c => c.UseServer("server1").WithName("broker1").AsDefault())
                    .Add<FakeBroker>(c => c.UseServer("server2").WithName("broker2").AsDefault()),
                Throws.InvalidOperationException);
        }

        [Test]
        public void GetTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2"))
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            var res = _config.Get("test2");
            Assert.That(res, Is.Not.Null);
            Assert.That(res, Is.InstanceOf<FakeBroker>());
            Assert.That(((FakeBroker)res).ServerName == "server2");
        }

        [Test]
        public void GenericGetTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2"))
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            var res = _config.Get<FakeBroker>("test2");
            Assert.That(res, Is.Not.Null);
            Assert.That(res.ServerName == "server2");
        }

        [Test]
        public void SingletonTest()
        {
            var conf = BrokersConfig.Instance;

            Assert.That(conf, Is.Not.Null);
        }

        [Test]
        public void GetEnumeratorTest()
        {
            _config
                .Add<FakeBroker>(c => c.UseServer("server1").WithName("test1"))
                .Add<FakeBroker>(c => c.UseServer("server2").WithName("test2"))
                .Add<FakeBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.GetEnumerator(), Is.Not.Null);
            Assert.That(_config.Any(), Is.True); // Any works on the IEnumerable
        }
    }
}
