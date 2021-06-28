// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Callbacks
{
    /// <summary>
    ///     Used to invoke the registered <see cref="IBrokerCallback" />.
    /// </summary>
    public interface IBrokerCallbacksInvoker
    {
        /// <summary>
        ///     Resolves and invokes all handlers of the specified type.
        /// </summary>
        /// <param name="action">
        ///     The action to be executed for each handler.
        /// </param>
        /// <param name="scopedServiceProvider">
        ///     The scoped <see cref="IServiceProvider" />. If not provided a new scope will be created.
        /// </param>
        /// <param name="invokeDuringShutdown">
        ///     Specifies whether the callback must be called even if the application is shutting down.
        /// </param>
        /// <typeparam name="THandler">
        ///     The type of the handler.
        /// </typeparam>
        void Invoke<THandler>(
            Action<THandler> action,
            IServiceProvider? scopedServiceProvider = null,
            bool invokeDuringShutdown = true);

        /// <summary>
        ///     Resolves and invokes all handlers of the specified type.
        /// </summary>
        /// <param name="action">
        ///     The action to be executed for each handler.
        /// </param>
        /// <param name="scopedServiceProvider">
        ///     The scoped <see cref="IServiceProvider" />. If not provided a new scope will be created.
        /// </param>
        /// <param name="invokeDuringShutdown">
        ///     Specifies whether the callback must be called even if the application is shutting down.
        /// </param>
        /// <typeparam name="THandler">
        ///     The type of the handler.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task InvokeAsync<THandler>(
            Func<THandler, Task> action,
            IServiceProvider? scopedServiceProvider = null,
            bool invokeDuringShutdown = true);
    }
}
