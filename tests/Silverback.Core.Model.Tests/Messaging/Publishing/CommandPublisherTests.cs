﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Model.TestTypes.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing
{
    public class CommandPublisherTests
    {
        private readonly ICommandPublisher _publisher;

        private int _receivedMessages;

        public CommandPublisherTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .UseModel()
                    .AddDelegateSubscriber((TestCommand _) => _receivedMessages++)
                    .AddDelegateSubscriber((TestCommandWithResult _) => new[] { 1, 2, 3 }));

            _publisher = serviceProvider.CreateScope().ServiceProvider
                .GetRequiredService<ICommandPublisher>();
        }

        [Fact]
        public async Task ExecuteAsync_Command_Executed()
        {
            await _publisher.ExecuteAsync(new TestCommand());

            _receivedMessages.Should().Be(1);
        }

        [Fact]
        public void Execute_Command_Executed()
        {
            _publisher.Execute(new TestCommand());

            _receivedMessages.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_CommandWithResult_ResultReturned()
        {
            var result = await _publisher.ExecuteAsync(new TestCommandWithResult());

            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Execute_CommandWithResult_ResultReturned()
        {
            var result = _publisher.Execute(new TestCommandWithResult());

            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public async Task ExecuteAsync_UnhandledCommand_ExceptionThrown()
        {
            Func<Task> act = () => _publisher.ExecuteAsync(new UnhandledCommand(), true);

            await act.Should().ThrowAsync<UnhandledMessageException>();
        }

        [Fact]
        public void Execute_UnhandledCommand_ExceptionThrown()
        {
            Action act = () => _publisher.Execute(new UnhandledCommand(), true);

            act.Should().Throw<UnhandledMessageException>();
        }

        [Fact]
        public async Task ExecuteAsync_UnhandledCommandWithResult_ExceptionThrown()
        {
            Func<Task> act = () => _publisher.ExecuteAsync(new UnhandledCommandWithResult(), true);

            await act.Should().ThrowAsync<UnhandledMessageException>();
        }

        [Fact]
        public void Execute_UnhandledCommandWithResult_ExceptionThrown()
        {
            Action act = () => _publisher.Execute(new UnhandledCommandWithResult(), true);

            act.Should().Throw<UnhandledMessageException>();
        }
    }
}
