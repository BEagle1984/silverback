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
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

[SuppressMessage("ReSharper", "LocalFunctionCanBeMadeStatic", Justification = "Test code")]
public partial class SilverbackBuilderFixture
{
    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenDelegateIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber((Delegate)(Func<object, object, Task>)((_, _) => Task.CompletedTask));

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenDelegateAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber(
                (Delegate)(Func<object, object, Task>)((_, _) => Task.CompletedTask),
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(
                _ =>
                {
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(
                _ =>
                {
                },
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(_ => Task.CompletedTask);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(
                _ => Task.CompletedTask,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(_ => 42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(
                _ => 42,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithOneServiceIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(
                (_, _) =>
                {
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithOneServiceAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(
                (_, _) =>
                {
                },
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithOneServiceReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>((_, _) => Task.CompletedTask);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithOneServiceReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(
                (_, _) => Task.CompletedTask,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithOneServiceReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>((_, _) => 42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithOneServiceReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object>(
                (_, _) => 42,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithTwoServicesIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(
                (_, _, _) =>
                {
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithTwoServicesAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(
                (_, _, _) =>
                {
                },
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithTwoServicesReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>((_, _, _) => Task.CompletedTask);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithTwoServicesReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(
                (_, _, _) => Task.CompletedTask,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithTwoServicesReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>((_, _, _) => 42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithTwoServicesReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object>(
                (_, _, _) => 42,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithThreeServicesIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(
                (_, _, _, _) =>
                {
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenActionWithThreeServicesAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(
                (_, _, _, _) =>
                {
                },
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithThreeServicesReturningTaskIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>((_, _, _, _) => Task.CompletedTask);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithThreeServicesReturningTaskAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(
                (_, _, _, _) => Task.CompletedTask,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithThreeServicesReturningObjectIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>((_, _, _, _) => 42);

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = true,
                Filters = Array.Empty<IMessageFilter>()
            });
    }

    [Fact]
    public void AddDelegateSubscriber_ShouldRegisterSubscriber_WhenFuncWithThreeServicesReturningObjectAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne, object, object, object>(
                (_, _, _, _) => 42,
                new DelegateSubscriptionOptions
                {
                    Exclusive = false
                });

        builder.BusOptions.Subscriptions.Should().HaveCount(1);
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Method.Should().NotBeNull();
        builder.BusOptions.Subscriptions.OfType<DelegateSubscription>().First().Options.Should().BeEquivalentTo(
            new DelegateSubscriptionOptions
            {
                Exclusive = false,
                Filters = Array.Empty<IMessageFilter>()
            });
    }
}
