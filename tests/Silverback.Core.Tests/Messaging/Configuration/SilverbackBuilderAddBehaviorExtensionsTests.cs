// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderAddBehaviorExtensionsTests
    {
        [Fact]
        public void AddTransientBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientBehavior(typeof(ChangeTestEventOneContentBehavior))
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddTransientBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientBehavior<ChangeTestEventOneContentBehavior>()
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddTransientBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientBehavior(_ => new ChangeTestEventOneContentBehavior())
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddScopedBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedBehavior(typeof(ChangeTestEventOneContentBehavior))
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddScopedBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedBehavior<ChangeTestEventOneContentBehavior>()
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddScopedBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedBehavior(_ => new ChangeTestEventOneContentBehavior())
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            using var serviceScope = serviceProvider.CreateScope();
            var publisher = serviceScope.ServiceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(typeof(ChangeTestEventOneContentBehavior))
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior<ChangeTestEventOneContentBehavior>()
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(_ => new ChangeTestEventOneContentBehavior())
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehavior_Instance_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(new ChangeTestEventOneContentBehavior())
                    .AddDelegateSubscriber<TestEventOne>(testEventOne => messages.Add(testEventOne)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }
    }
}
