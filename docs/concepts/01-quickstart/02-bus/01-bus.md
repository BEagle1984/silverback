# Enabling the Bus

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
