// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing
{
    /// <summary>
    ///     The purpose of this class is to ensure that the publisher is still working when
    ///     the broker subscribers are added.
    /// </summary>
    public class PublisherTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceProvider _scopedServiceProvider;

        public PublisherTests()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            _scopedServiceProvider = _serviceProvider.CreateScope().ServiceProvider;
        }

        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            _serviceProvider.GetService<BusConfigurator>()
                .Subscribe<object>(_ => "response")
                .Subscribe<object>(_ => "response2");

            var results = _scopedServiceProvider.GetService<IPublisher>().Publish<string>("test");

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            _serviceProvider.GetService<BusConfigurator>()
                .Subscribe<object>(_ => "response")
                .Subscribe<object>(_ => "response2");

            var results = await _scopedServiceProvider.GetService<IPublisher>().PublishAsync<string>("test");

            results.Should().Equal("response", "response2");
        }
    }
}