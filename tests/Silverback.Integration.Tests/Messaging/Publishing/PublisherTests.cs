using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing
{
    /// <summary>
    /// The purpose of this class is to ensure that the publisher is still working when
    /// the broker subsribers are added.
    /// </summary>
    public class PublisherTests
    {
        private readonly ServiceProvider _serviceProvider;

        public PublisherTests()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services
                .AddBus()
                .AddBroker<TestBroker>();

            _serviceProvider = services.BuildServiceProvider();
        }
        
        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            _serviceProvider.GetService<BusConfigurator>()
                .Subscribe<object>(_ => "response")
                .Subscribe<object>(_ => "response2");

            var results = _serviceProvider.GetService<IPublisher>().Publish<string>("test");

            results.Should().Equal("response", "response2");
        }


        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            _serviceProvider.GetService<BusConfigurator>()
                .Subscribe<object>(_ => "response")
                .Subscribe<object>(_ => "response2");

            var results = await _serviceProvider.GetService<IPublisher>().PublishAsync<string>("test");

            results.Should().Equal("response", "response2");
        }
    }
}
