// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "Makes test code more obvious")]
public partial class SilverbackBuilderFixture
{
    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(typeof(TestSubscriber));

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(typeof(TestSubscriber), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(
                typeof(TestSubscriber),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>();

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>(false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>(
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(typeof(TestSubscriber), _ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(typeof(TestSubscriber), _ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber(
                typeof(TestSubscriber),
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>(_ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>(_ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddTransientSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddTransientSubscriber<TestSubscriber>(
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Transient);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(typeof(TestSubscriber));

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(typeof(TestSubscriber), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(
                typeof(TestSubscriber),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>();

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>(false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>(
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(typeof(TestSubscriber), _ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(typeof(TestSubscriber), _ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber(
                typeof(TestSubscriber),
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>(_ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>(_ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddScopedSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddScopedSubscriber<TestSubscriber>(
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Scoped);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber));

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(
                typeof(TestSubscriber),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>();

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber), _ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber), _ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(
                typeof(TestSubscriber),
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(_ => new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(_ => new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndFactoryAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(
                _ => new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationFactory != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndInstanceAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber), new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndInstanceAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(typeof(TestSubscriber), new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenTypeAndInstanceAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber(
                typeof(TestSubscriber),
                new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndInstanceAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(new TestSubscriber());

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndInstanceAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(new TestSubscriber(), false);

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSingletonSubscriber_ShouldRegisterSubscriber_WhenGenericTypeParameterAndInstanceAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSingletonSubscriber<TestSubscriber>(
                new TestSubscriber(),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.Services.Should().Contain(
            service => service.ServiceType == typeof(TestSubscriber) &&
                       service.ImplementationInstance != null &&
                       service.Lifetime == ServiceLifetime.Singleton);
        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().Should().Contain(
            subscription => subscription.SubscriberType == typeof(TestSubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods == false &&
                            subscription.Options.IsExclusive == false &&
                            subscription.Options.Filters.Count == 0);
    }

    [UsedImplicitly]
    private class TestSubscriber
    {
    }
}
