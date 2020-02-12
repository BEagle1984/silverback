// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Examples.Common.Subscribers;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Silverback
{
    public static class SilverbackBuilderExtensions
    {
        public static ISilverbackBuilder AddCommonSubscribers(this ISilverbackBuilder builder) => builder
            .AddScopedSubscriber<SampleEventsSubscriber>()
            .AddScopedSubscriber<MultipleGroupsSubscriber>()
            .AddScopedSubscriber<LegacyMessagesSubscriber>()
            .AddScopedSubscriber<SilverbackEventsSubscriber>();
    }
}