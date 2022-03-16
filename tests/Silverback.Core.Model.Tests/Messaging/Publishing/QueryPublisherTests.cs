// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Model.TestTypes.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public class QueryPublisherTests
{
    private readonly IQueryPublisher _publisher;

    public QueryPublisherTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseModel()
                .AddDelegateSubscriber2<TestQuery, int[]>(Handle));

        static int[] Handle(TestQuery message) => new[] { 1, 2, 3 };

        _publisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IQueryPublisher>();
    }

    [Fact]
    public async Task ExecuteAsync_Query_ResultReturned()
    {
        IEnumerable<int> result = await _publisher.ExecuteAsync(new TestQuery());

        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Execute_Query_ResultReturned()
    {
        IEnumerable<int> result = _publisher.Execute(new TestQuery());

        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteAsync_UnhandledQuery_ExceptionThrown()
    {
        Func<Task> act = () => _publisher.ExecuteAsync(new UnhandledQuery());

        await act.Should().ThrowAsync<UnhandledMessageException>();
    }

    [Fact]
    public void Execute_UnhandledQuery_ExceptionThrown()
    {
        Action act = () => _publisher.Execute(new UnhandledQuery());

        act.Should().Throw<UnhandledMessageException>();
    }
}
