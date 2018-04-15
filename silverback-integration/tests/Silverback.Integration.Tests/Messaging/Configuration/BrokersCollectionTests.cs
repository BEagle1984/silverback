using System.Linq;
using NUnit.Framework;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class BrokersCollectionTests
    {
        private BrokersCollection _config;

        [SetUp]
        public void Setup()
        {
            _config = new BrokersCollection();
        }

        [Test]
        public void AddTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Count, Is.EqualTo(3));
        }

        [Test]
        public void DefaultTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)_config.Default).ServerName, Is.EqualTo("server1"));
        }

        [Test]
        public void AddDefaultTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2").AsDefault());
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)_config.Default).ServerName, Is.EqualTo("server2"));
        }

        [Test]
        public void AddNoNameTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1"));

            Assert.That(_config.Default, Is.Not.Null);
            Assert.That(_config.Default, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)_config.Default).ServerName, Is.EqualTo("server1"));
        }

        [Test]
        public void AddDuplicatedNameTest()
        {
            Assert.That(
                () =>
                {
                    _config.Add<TestBroker>(c => c.UseServer("server1").WithName("duplicate"));
                    _config.Add<TestBroker>(c => c.UseServer("server2").WithName("duplicate"));
                },
                Throws.InvalidOperationException);
        }


        [Test]
        public void AddDuplicatedDefaultTest()
        {
            Assert.That(
                () =>
                {
                    _config.Add<TestBroker>(c => c.UseServer("server1").WithName("broker1").AsDefault());
                    _config.Add<TestBroker>(c => c.UseServer("server2").WithName("broker2").AsDefault());
                },
                Throws.InvalidOperationException);
        }

        [Test]
        public void GetTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            var res = _config.Get("test2");
            Assert.That(res, Is.Not.Null);
            Assert.That(res, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)res).ServerName, Is.EqualTo("server2"));
        }

        [Test]
        public void GetTypedTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            var res = _config.Get<TestBroker>("test2");
            Assert.That(res, Is.Not.Null);
            Assert.That(res.ServerName, Is.EqualTo("server2"));
        }

        [Test]
        public void GetDefaultTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2").AsDefault());
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            var res = _config.Get();
            Assert.That(res, Is.Not.Null);
            Assert.That(res, Is.EqualTo(_config.Default));
            Assert.That(res, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)res).ServerName, Is.EqualTo("server2"));
        }

        [Test]
        public void GetImplicitDefaultTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3"));

            var res = _config.Get();
            Assert.That(res, Is.Not.Null);
            Assert.That(res, Is.EqualTo(_config.Default));
            Assert.That(res, Is.InstanceOf<TestBroker>());
            Assert.That(((TestBroker)res).ServerName, Is.EqualTo("server1"));
        }

        [Test]
        public void GetEnumeratorTest()
        {
            _config.Add<TestBroker>(c => c.UseServer("server1").WithName("test1"));
            _config.Add<TestBroker>(c => c.UseServer("server2").WithName("test2"));
            _config.Add<TestBroker>(c => c.UseServer("server3").WithName("test3"));

            Assert.That(_config.GetEnumerator(), Is.Not.Null);
            Assert.That(_config.Any(), Is.True); // Any works on the IEnumerable
        }
    }
}
