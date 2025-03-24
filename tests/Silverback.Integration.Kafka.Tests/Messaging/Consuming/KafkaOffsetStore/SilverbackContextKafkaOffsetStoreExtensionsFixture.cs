// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

public class SilverbackContextKafkaOffsetStoreExtensionsFixture
{
    [Fact]
    public void SetKafkaOffsetStoreScope_ShouldSetScope()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaOffsetStoreScope scope = new(Substitute.For<IKafkaOffsetStore>(), ConsumerPipelineContextHelper.CreateSubstitute());

        context.SetKafkaOffsetStoreScope(scope);

        context.GetKafkaOffsetStoreScope().ShouldBe(scope);
    }

    [Fact]
    public void SetKafkaOffsetStoreScope_ShouldThrow_WhenAlreadySet()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaOffsetStoreScope scope1 = new(Substitute.For<IKafkaOffsetStore>(), ConsumerPipelineContextHelper.CreateSubstitute());
        KafkaOffsetStoreScope scope2 = new(Substitute.For<IKafkaOffsetStore>(), ConsumerPipelineContextHelper.CreateSubstitute());

        context.SetKafkaOffsetStoreScope(scope1);

        Assert.Throws<InvalidOperationException>(() => context.SetKafkaOffsetStoreScope(scope2));
    }

    [Fact]
    public void SetKafkaOffsetStoreScope_ShouldNotThrow_WhenSameScopeIsSetAgain()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaOffsetStoreScope scope = new(Substitute.For<IKafkaOffsetStore>(), ConsumerPipelineContextHelper.CreateSubstitute());

        context.SetKafkaOffsetStoreScope(scope);

        Action act = () => context.SetKafkaOffsetStoreScope(scope);
        act.ShouldNotThrow();
    }
}
