// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public partial class SilverbackBuilderFixture
{
    [Fact]
    public void AddTransientBehavior_ShouldAddBehavior_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddTransientBehavior(typeof(TestBehavior));

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddTransientBehavior_ShouldAddBehavior_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddTransientBehavior<TestBehavior>();

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddTransientBehavior_ShouldAddBehavior_WhenFactoryIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddTransientBehavior(_ => new TestBehavior());

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationFactory.Should().NotBeNull();
        descriptors[0].ImplementationFactory!.Invoke(Substitute.For<IServiceProvider>()).Should().BeAssignableTo<TestBehavior>();
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddScopedBehavior_ShouldAddBehavior_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddScopedBehavior(typeof(TestBehavior));

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddScopedBehavior_ShouldAddBehavior_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddScopedBehavior<TestBehavior>();

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddScopedBehavior_ShouldAddBehavior_WhenFactoryIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddScopedBehavior(_ => new TestBehavior());

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationFactory.Should().NotBeNull();
        descriptors[0].ImplementationFactory!.Invoke(Substitute.For<IServiceProvider>()).Should().BeAssignableTo<TestBehavior>();
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSingletonBehavior_ShouldAddBehavior_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddSingletonBehavior(typeof(TestBehavior));

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSingletonBehavior_ShouldAddBehavior_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddSingletonBehavior<TestBehavior>();

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationType.Should().Be(typeof(TestBehavior));
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSingletonBehavior_ShouldAddBehavior_WhenFactoryIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();

        builder.AddSingletonBehavior(_ => new TestBehavior());

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationFactory.Should().NotBeNull();
        descriptors[0].ImplementationFactory!.Invoke(Substitute.For<IServiceProvider>()).Should().BeAssignableTo<TestBehavior>();
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSingletonBehavior_ShouldAddBehavior_WhenInstanceIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection().AddSilverback();
        TestBehavior instance = new();

        builder.AddSingletonBehavior(instance);

        IReadOnlyList<ServiceDescriptor> descriptors = builder.Services.GetAll<IBehavior>();
        descriptors.Should().HaveCount(1);
        descriptors[0].ImplementationInstance.Should().NotBeNull();
        descriptors[0].ImplementationInstance.Should().Be(instance);
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    private class TestBehavior : IBehavior
    {
        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next) => next.Invoke(message);
    }
}
