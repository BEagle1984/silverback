// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

[SuppressMessage("ReSharper", "LocalFunctionCanBeMadeStatic", Justification = "Test code")]
public partial class SilverbackBuilderFixture
{
    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle);

        void Handle(TestEventOne message)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        void Handle(TestEventOne message)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle);

        Task Handle(TestEventOne message) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle);

        ValueTask Handle(TestEventOne message) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriber`1");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        int Handle(TestEventOne message) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        Task<int> Handle(TestEventOne message) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        ValueTask<int> Handle(TestEventOne message) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResult`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenActionIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle);

        void Handle(TestEventOne message, object service)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenActionAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        void Handle(TestEventOne message, object service)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle);

        Task Handle(TestEventOne message, object service) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle);

        ValueTask Handle(TestEventOne message, object service) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT2`2");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        int Handle(TestEventOne message, object service) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT2`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenActionIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle);

        void Handle(TestEventOne message, object service1, object service2)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenActionAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        void Handle(TestEventOne message, object service1, object service2)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle);

        Task Handle(TestEventOne message, object service1, object service2) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service1, object service2) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle);

        ValueTask Handle(TestEventOne message, object service1, object service2) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service1, object service2) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT3`3");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        int Handle(TestEventOne message, object service1, object service2) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service1, object service2) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service1, object service2) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service1, object service2) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service1, object service2) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service1, object service2) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT3`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenActionIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle);

        void Handle(TestEventOne message, object service1, object service2, object service3)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenActionAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        void Handle(TestEventOne message, object service1, object service2, object service3)
        {
        }

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle);

        Task Handle(TestEventOne message, object service1, object service2, object service3) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service1, object service2, object service3) => Task.CompletedTask;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle);

        ValueTask Handle(TestEventOne message, object service1, object service2, object service3) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service1, object service2, object service3) => default;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberT4`4");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        int Handle(TestEventOne message, object service1, object service2, object service3) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service1, object service2, object service3) => 42;

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service1, object service2, object service3) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service1, object service2, object service3) => Task.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("AsyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service1, object service2, object service3) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service1, object service2, object service3) => ValueTask.FromResult(42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.Should().Be("SyncOrAsyncSubscriberWithResultT4`5");
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                IsExclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [SuppressMessage("", "CA1812", Justification = "Class used for testing")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used for testing")]
    private class TestEventOne
    {
    }
}
