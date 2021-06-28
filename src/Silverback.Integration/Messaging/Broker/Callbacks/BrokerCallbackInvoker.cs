// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Callbacks
{
    internal class BrokerCallbackInvoker : IBrokerCallbacksInvoker
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ISilverbackLogger<BrokerCallbackInvoker> _logger;

        private List<Type>? _callbackTypes;

        private bool _appStopping;

        public BrokerCallbackInvoker(
            IServiceScopeFactory serviceScopeFactory,
            IHostApplicationLifetime applicationLifetime,
            ISilverbackLogger<BrokerCallbackInvoker> logger)
        {
            _logger = logger;
            _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));

            applicationLifetime.ApplicationStopping.Register(() => _appStopping = true);
        }

        /// <inheritdoc cref="IBrokerCallbacksInvoker.Invoke{THandler}"/>
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
                _logger.LogCallbackHandlerError(ex);
                throw;
            }
        }

        /// <inheritdoc cref="IBrokerCallbacksInvoker.InvokeAsync{THandler}"/>
        public async Task InvokeAsync<TCallback>(
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
                _logger.LogCallbackHandlerError(ex);
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
                throw new BrokerCallbackInvocationException(
                    "An exception has been thrown by the broker callback. " +
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
                throw new BrokerCallbackInvocationException(
                    "An exception has been thrown by the broker callback. " +
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
                if (scopedServiceProvider == null)
                {
                    scope = _serviceScopeFactory.CreateScope();
                    scopedServiceProvider = scope.ServiceProvider;
                }

                var services = GetCallbacks<TCallback>(scopedServiceProvider);

                foreach (var service in services)
                {
                    TryInvoke(service, action);
                }
            }
            catch (BrokerCallbackInvocationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BrokerCallbackInvocationException(
                    $"Error occurred invoking the callbacks of type {typeof(TCallback).Name}. " +
                    "See inner exception for details.",
                    ex);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private async Task InvokeCoreAsync<TCallback>(
            Func<TCallback, Task> action,
            IServiceProvider? scopedServiceProvider,
            bool invokeDuringShutdown)
        {
            if (!HasAny<TCallback>() || _appStopping && !invokeDuringShutdown)
                return;

            IServiceScope? scope = null;

            try
            {
                if (scopedServiceProvider == null)
                {
                    scope = _serviceScopeFactory.CreateScope();
                    scopedServiceProvider = scope.ServiceProvider;
                }

                var services = GetCallbacks<TCallback>(scopedServiceProvider);

                foreach (var service in services)
                {
                    await TryInvokeAsync(service, action).ConfigureAwait(false);
                }
            }
            catch (BrokerCallbackInvocationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BrokerCallbackInvocationException(
                    $"Error occurred invoking the callbacks of type {typeof(TCallback).Name}. " +
                    "See inner exception for details.",
                    ex);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private IEnumerable<TCallback> GetCallbacks<TCallback>(IServiceProvider scopedServiceProvider)
        {
            var callbacks = scopedServiceProvider.GetServices<IBrokerCallback>().ToList();

            if (_callbackTypes == null)
                _callbackTypes = callbacks.Select(callback => callback.GetType()).ToList();

            return callbacks.OfType<TCallback>().SortBySortIndex();
        }

        private bool HasAny<TCallback>()
        {
            if (_callbackTypes == null)
                return true;

            return _callbackTypes.Any(type => typeof(TCallback).IsAssignableFrom(type));
        }
    }
}
