// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "Makes test code more obvious")]
public partial class SilverbackBuilderFixture
{
    private interface ISubscriber;

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers(typeof(ISubscriber));

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenTypeAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers(typeof(ISubscriber), false);

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            !subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenTypeAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers(
                typeof(ISubscriber),
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            !subscription.Options.AutoSubscribeAllPublicMethods &&
                            !subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers<ISubscriber>();

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenGenericArgumentAndPublicMethodsFlagAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers<ISubscriber>(false);

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            !subscription.Options.AutoSubscribeAllPublicMethods &&
                            subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }

    [Fact]
    public void AddSubscribers_ShouldRegisterSubscriber_WhenGenericArgumentAndOptionsAreSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .AddSubscribers<ISubscriber>(
                new TypeSubscriptionOptions
                {
                    AutoSubscribeAllPublicMethods = false,
                    IsExclusive = false
                });

        builder.BusOptions.Subscriptions.OfType<TypeSubscription>().ShouldContain(
            subscription => subscription.SubscriberType == typeof(ISubscriber) &&
                            !subscription.Options.AutoSubscribeAllPublicMethods &&
                            !subscription.Options.IsExclusive &&
                            subscription.Options.Filters.Count == 0);
    }
}
