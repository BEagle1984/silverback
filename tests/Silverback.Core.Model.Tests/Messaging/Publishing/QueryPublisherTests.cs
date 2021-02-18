// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Model.TestTypes.Messages;
using Silverback.Tests.Core.Model.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing
{
    public class QueryPublisherTests
    {
        private readonly IQueryPublisher _publisher;

        public QueryPublisherTests()
        {
            var services = new ServiceCollection();
            services
                .AddSilverback()
                .UseModel()
                .AddSingletonSubscriber(_ => new QueriesHandler());

            services.AddLoggerSubstitute();

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _publisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IQueryPublisher>();
        }

        [Fact]
        public async Task ExecuteAsync_ListQuery_EnumerableReturned()
        {
            var result = await _publisher.ExecuteAsync(new ListQuery { Count = 3 });

            result.Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public void Execute_ListQuery_EnumerableReturned()
        {
            var result = _publisher.Execute(new ListQuery { Count = 3 });

            result.Should().BeEquivalentTo(1, 2, 3);
        }
    }
}
