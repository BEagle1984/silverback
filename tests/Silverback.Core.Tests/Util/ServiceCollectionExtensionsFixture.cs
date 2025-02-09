// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ServiceCollectionExtensionsFixture
{
    private interface IService;

    private interface IOtherService;

    [Fact]
    public void ContainsAny_ShouldReturnTrue_WhenTypeSpecifiedViaTypeParameterIsAlreadyRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        bool result = services.ContainsAny(typeof(IService));

        result.ShouldBeTrue();
    }

    [Fact]
    public void ContainsAny_ShouldReturnTrue_WhenTypeSpecifiedViaGenericArgumentIsAlreadyRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        bool result = services.ContainsAny<IService>();

        result.ShouldBeTrue();
    }

    [Fact]
    public void ContainsAny_ShouldReturnFalse_WhenTypeSpecifiedViaTypeParameterIsNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        bool result = services.ContainsAny(typeof(IOtherService));

        result.ShouldBeFalse();
    }

    [Fact]
    public void ContainsAny_ShouldReturnFalse_WhenTypeSpecifiedViaGenericArgumentIsNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        bool result = services.ContainsAny<IOtherService>();

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnInstanceOfRegisteredSingleton_WhenTypeIsSpecified()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService>(new Service());

        object? result = services.GetSingletonServiceInstance(typeof(IService));

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Service>();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnInstanceOfRegisteredSingleton_WhenGenericArgumentIsSpecified()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService>(new Service());

        IService? result = services.GetSingletonServiceInstance<IService>();

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Service>();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaTypeParameterIsNotRegisteredAsSingleton()
    {
        ServiceCollection services = [];
        services.AddTransient<IService, Service>();

        object? result = services.GetSingletonServiceInstance(typeof(IService));

        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaGenericArgumentIsNotRegisteredAsSingleton()
    {
        ServiceCollection services = [];
        services.AddTransient<IService, Service>();

        IService? result = services.GetSingletonServiceInstance<IService>();

        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaTypeParameterIsNotRegisteredWithImplementationInstance()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        object? result = services.GetSingletonServiceInstance(typeof(IService));

        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaGenericArgumentIsNotRegisteredWithImplementationInstance()
    {
        ServiceCollection services = [];
        services.AddSingleton<IService, Service>();

        IService? result = services.GetSingletonServiceInstance<IService>();

        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaTypeParameterIsNotRegistered()
    {
        ServiceCollection services = [];

        object? result = services.GetSingletonServiceInstance(typeof(IService));

        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingletonServiceInstance_ShouldReturnNull_WhenTypeSpecifiedViaGenericArgumentIsNotRegistered()
    {
        ServiceCollection services = [];

        IService? result = services.GetSingletonServiceInstance<IService>();

        result.ShouldBeNull();
    }

    private class Service : IService;
}
