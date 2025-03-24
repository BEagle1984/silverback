// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Callbacks;

internal sealed class BrokerClientCallbacksInvoker : IBrokerClientCallbacksInvoker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ISilverbackLogger<BrokerClientCallbacksInvoker> _logger;

    private readonly ConcurrentDictionary<Type, bool> _hasCallbacks = new();

    private List<Type>? _callbackTypes;

    private bool _appStopping;

    public BrokerClientCallbacksInvoker(
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime applicationLifetime,
        ISilverbackLogger<BrokerClientCallbacksInvoker> logger)
    {
        _logger = logger;
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));

        applicationLifetime.ApplicationStopping.Register(() => _appStopping = true);
    }

    /// <inheritdoc cref="IBrokerClientCallbacksInvoker.Invoke{THandler}" />
    public void Invoke<TCallback>(
        Action<TCallback> action,
        IServiceProvider? scopedServiceProvider = null,
        bool invokeDuringShutdown = true)
    {
        try
        {
            InvokeCore(action, scopedServiceProvider, invokeDuringShutdown);
        }
        catch (Exception ex)
        {
            _logger.LogCallbackError(ex);
            throw;
        }
    }

    /// <inheritdoc cref="IBrokerClientCallbacksInvoker.InvokeAsync{THandler}" />
    public async ValueTask InvokeAsync<TCallback>(
        Func<TCallback, Task> action,
        IServiceProvider? scopedServiceProvider = null,
        bool invokeDuringShutdown = true)
    {
        try
        {
            await InvokeCoreAsync(action, scopedServiceProvider, invokeDuringShutdown).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCallbackError(ex);
            throw;
        }
    }

    private static void TryInvoke<TCallback>(TCallback service, Action<TCallback> action)
    {
        try
        {
            action.Invoke(service);
        }
        catch (Exception ex)
        {
            throw new BrokerClientCallbackInvocationException(
                "An exception has been thrown by the client callback. " +
                "See inner exception for details.",
                ex);
        }
    }

    private static async Task TryInvokeAsync<TCallback>(TCallback service, Func<TCallback, Task> action)
    {
        try
        {
            await action.Invoke(service).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new BrokerClientCallbackInvocationException(
                "An exception has been thrown by the client callback. " +
                "See inner exception for details.",
                ex);
        }
    }

    private void InvokeCore<TCallback>(
        Action<TCallback> action,
        IServiceProvider? scopedServiceProvider,
        bool invokeDuringShutdown)
    {
        if (!HasAny<TCallback>() || _appStopping && !invokeDuringShutdown)
            return;

        IServiceScope? scope = null;

        try
        {
            (IEnumerable<TCallback> services, scope) = GetCallbacks<TCallback>(scopedServiceProvider, scope);

            foreach (TCallback service in services)
            {
                TryInvoke(service, action);
            }
        }
        catch (BrokerClientCallbackInvocationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BrokerClientCallbackInvocationException(
                $"Error occurred invoking the callbacks of type {typeof(TCallback).Name}. " +
                "See inner exception for details.",
                ex);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private async ValueTask InvokeCoreAsync<TCallback>(
        Func<TCallback, Task> action,
        IServiceProvider? scopedServiceProvider,
        bool invokeDuringShutdown)
    {
        if (!HasAny<TCallback>() || _appStopping && !invokeDuringShutdown)
            return;

        IServiceScope? scope = null;

        try
        {
            (IEnumerable<TCallback> services, scope) = GetCallbacks<TCallback>(scopedServiceProvider, scope);

            foreach (TCallback service in services)
            {
                await TryInvokeAsync(service, action).ConfigureAwait(false);
            }
        }
        catch (BrokerClientCallbackInvocationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BrokerClientCallbackInvocationException(
                $"Error occurred invoking the callbacks of type {typeof(TCallback).Name}. " +
                "See inner exception for details.",
                ex);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private (IEnumerable<TCallback> Services, IServiceScope? Scope) GetCallbacks<TCallback>(IServiceProvider? scopedServiceProvider, IServiceScope? scope)
    {
        if (scopedServiceProvider == null)
        {
            scope = TryCreateServiceScope();

            if (scope == null)
                return ([], null);

            scopedServiceProvider = scope.ServiceProvider;
        }

        IEnumerable<TCallback> services = GetCallbacks<TCallback>(scopedServiceProvider);
        return (services, scope);
    }

    private IEnumerable<TCallback> GetCallbacks<TCallback>(IServiceProvider scopedServiceProvider)
    {
        List<IBrokerClientCallback> callbacks = scopedServiceProvider.GetServices<IBrokerClientCallback>().ToList();

        _callbackTypes ??= callbacks.Select(callback => callback.GetType()).ToList();

        return callbacks.OfType<TCallback>().SortBySortIndex();
    }

    private IServiceScope? TryCreateServiceScope()
    {
        try
        {
            return _serviceScopeFactory.CreateScope();
        }
        catch (ObjectDisposedException)
        {
            // The application is probably being shutdown. Ignore the error to avoid polluting the logs.
            return null;
        }
    }

    private bool HasAny<TCallback>()
    {
        // If the types haven't been initialized yet (very first call), just return true to go through the procedure
        // once and load them.
        if (_callbackTypes == null)
            return true;

        return _hasCallbacks.GetOrAdd(
            typeof(TCallback),
            static (_, types) => types.Exists(type => typeof(TCallback).IsAssignableFrom(type)),
            _callbackTypes);
    }
}
