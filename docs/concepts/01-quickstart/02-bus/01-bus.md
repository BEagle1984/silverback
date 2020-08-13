---
uid: enabling-silverback
---

# Enabling Silverback

Silverback's main component is the internal in-memory message bus and pretty much all other features are built on top of that.

The first mandatory step to start using Silverback is to register the core services (internal bus) with the .net core dependency injection.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilverback();
    }
}
```

# Configuring Silverback

The `AddSilverback` method highlighted in the previous chapter returns an `ISilverbackBuilder` that exposes all the methods needed to configure Silverback and wire everything up.

The several configuration options will are exhaustively presented in each dedicated section of this documentation but here is a basic sample startup.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpointsConfigurator<OrdersEndpointsConfigurator>()
            .AddEndpointsConfigurator<ProductsEndpointsConfigurator>()
            .AddScopedSubscriber<OrderEventsSubscriber>()
            .AddScopedSubscriber<ProductEventsSubscriber>();
    }
}
```

Note that `AddSilverback` should be called only once but you can use the `ConfigureSilverback` extension method on the `IServiceCollection` to retrieve the `ISilverbackBuilder` instance once again.

# [Startup](#tab/configure-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka());
            
        services
            .AddOrdersFeature()
            .AddProductsFeature();
    }
}
```
# [Orders Feature](#tab/configure-feature1)
```csharp
public static class OrdersFeatureConfigurator
{
    public static void AddOrdersFeature(this IServiceCollection services)
    {
        services
            .ConfigureSilverback()
            .AddEndpointsConfigurator<OrdersEndpointsConfigurator>()
            .AddScopedSubscriber<OrderEventsSubscriber>();
    }
}
```
# [Products Feature](#tab/configure-feature2)
```csharp
public static class ProductsFeatureConfigurator
{
    public static void AddProductsFeature(this IServiceCollection services)
    {
        services
            .ConfigureSilverback()
            .AddEndpointsConfigurator<ProductsEndpointsConfigurator>()
            .AddScopedSubscriber<ProductEventsSubscriber>();
    }
}
```