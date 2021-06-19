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

        /// <summary>
        ///     Resolves and invokes all callbacks of the specified type.
        /// </summary>
        /// <param name="action">
        ///     The action to be executed for each callback.
        /// </param>
        /// <param name="scopedServiceProvider">
        ///     The scoped <see cref="IServiceProvider" />. If not provided a new scope will be created.
        /// </param>
        /// <typeparam name="TCallback">
        ///     The type of the callback.
        /// </typeparam>
        public void Invoke<TCallback>(
            Action<TCallback> action,
            IServiceProvider? scopedServiceProvider = null)
        {
            try
            {
                InvokeCore(action, scopedServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogCallbackHandlerError(ex);
                throw;
            }
        }

        /// <summary>
        ///     Resolves and invokes all callbacks of the specified type.
        /// </summary>
        /// <param name="action">
        ///     The action to be executed for each callback.
        /// </param>
        /// <param name="scopedServiceProvider">
        ///     The scoped <see cref="IServiceProvider" />. If not provided a new scope will be created.
        /// </param>
        /// <typeparam name="TCallback">
        ///     The type of the callback.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task InvokeAsync<TCallback>(
            Func<TCallback, Task> action,
            IServiceProvider? scopedServiceProvider = null)
        {
            try
            {
                await InvokeCoreAsync(action, scopedServiceProvider).ConfigureAwait(false);
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
            IServiceProvider? scopedServiceProvider)
        {
            if (_appStopping || !HasAny<TCallback>())
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
            IServiceProvider? scopedServiceProvider)
        {
            if (_appStopping || !HasAny<TCallback>())
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
