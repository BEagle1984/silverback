// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.Model.TestTypes.Messages;
using Silverback.Tests.Core.Model.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing
{
    public class QueryPublisherTests
    {
        private readonly IQueryPublisher _publisher;

        public QueryPublisherTests()
        { 
            var services = new ServiceCollection();
            services.AddBus(options => options.UseModel());

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                services.AddSingleton<ISubscriber>(_ => new QueriesHandler());

            var serviceProvider = services.BuildServiceProvider();

            _publisher =  serviceProvider.GetRequiredService<IQueryPublisher>();
        }

        [Fact]
        public async Task ExecuteAsync_ListQuery_EnumerableReturned()
        {
            var result = await _publisher.ExecuteAsync(new ListQuery {Count = 3});

            result.Should().BeEquivalentTo(1, 2, 3);
        }
    }
}