// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

[SuppressMessage("ReSharper", "LocalFunctionCanBeMadeStatic", Justification = "Test code")]
[SuppressMessage("Style", "IDE0062:Make local function \'static\'", Justification = "Test code")]
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle);

        Task Handle(TestEventOne message) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle);

        ValueTask Handle(TestEventOne message) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriber`1");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        int Handle(TestEventOne message) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        Task<int> Handle(TestEventOne message) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle);

        ValueTask<int> Handle(TestEventOne message) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResult`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle);

        Task Handle(TestEventOne message, object service) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle);

        ValueTask Handle(TestEventOne message, object service) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT2`2");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        int Handle(TestEventOne message, object service) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithOneService_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT2`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle);

        Task Handle(TestEventOne message, object service1, object service2) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service1, object service2) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle);

        ValueTask Handle(TestEventOne message, object service1, object service2) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service1, object service2) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT3`3");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        int Handle(TestEventOne message, object service1, object service2) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service1, object service2) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service1, object service2) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service1, object service2) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service1, object service2) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithTwoServices_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service1, object service2) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT3`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
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

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle);

        Task Handle(TestEventOne message, object service1, object service2, object service3) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task Handle(TestEventOne message, object service1, object service2, object service3) => Task.CompletedTask;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle);

        ValueTask Handle(TestEventOne message, object service1, object service2, object service3) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask Handle(TestEventOne message, object service1, object service2, object service3) => default;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberT4`4");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        int Handle(TestEventOne message, object service1, object service2, object service3) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        int Handle(TestEventOne message, object service1, object service2, object service3) => 42;

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        Task<int> Handle(TestEventOne message, object service1, object service2, object service3) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        Task<int> Handle(TestEventOne message, object service1, object service2, object service3) => Task.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("AsyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskWithResultIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle);

        ValueTask<int> Handle(TestEventOne message, object service1, object service2, object service3) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = true,
                Filters = []
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriberWithThreeServices_WhenFuncReturningValueTaskWithResultAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object, int>(Handle, new DelegateSubscriptionOptions { IsExclusive = false });

        ValueTask<int> Handle(TestEventOne message, object service1, object service2, object service3) => ValueTask.FromResult(42);

        builder.MediatorOptions.Subscriptions.Count.ShouldBe(1);
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().SubscriberType.Name.ShouldBe("SyncOrAsyncSubscriberWithResultT4`5");
        builder.MediatorOptions.Subscriptions.OfType<TypeSubscription>().First().Options.ShouldBeEquivalentTo(
            new TypeSubscriptionOptions
            {
                IsExclusive = false,
                Filters = []
            });
    }

    private class TestEventOne;
}
