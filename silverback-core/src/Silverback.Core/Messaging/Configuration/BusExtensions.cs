using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;

/// <summary>
/// Contains a set of extention methods useful to setup the <see cref="IBus"/>.
/// </summary>
public static class BusExtensions
{
    private const string ItemsKeyPrefix = "Silverback.Configuration.";

    #region Type Factory (IoC)

    /// <summary>
    /// Sets the type factory.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <param name="typeFactory">The type factory.</param>
    /// <returns></returns>
    internal static ITypeFactory SetTypeFactory(this IBus bus, ITypeFactory typeFactory)
        => (ITypeFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ITypeFactory", typeFactory, (_, _) => typeFactory);

    /// <summary>
    /// Gets the currently configured type factory.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <returns></returns>
    internal static ITypeFactory GetTypeFactory(this IBus bus)
    {
        if (bus.Items.TryGetValue(ItemsKeyPrefix + "ITypeFactory", out object loggerFactory))
            return loggerFactory as ITypeFactory;

        return null;
    }

    #endregion

    #region Logger Factory

    /// <summary>
    /// Sets the logger factory.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns></returns>
    internal static ILoggerFactory SetLoggerFactory(this IBus bus, ILoggerFactory loggerFactory)
        => (ILoggerFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ILoggerFactory", loggerFactory, (_, _) => loggerFactory);

    /// <summary>
    /// Gets the currently configured logger factory.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <returns></returns>
    internal static ILoggerFactory GetLoggerFactory(this IBus bus)
    {
        if (bus.Items.TryGetValue(ItemsKeyPrefix + "ILoggerFactory", out object loggerFactory))
            return loggerFactory as ILoggerFactory;

        return null;
    }

    #endregion
}