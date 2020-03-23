// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public interface IBrokerOptionsConfigurator<TBroker>
        where TBroker : IBroker
    {
        /// <summary>
        ///     Called while registering the broker to configure the broker-specific services and options
        ///     (e.g. behaviors).
        /// </summary>
        void Configure(IBrokerOptionsBuilder options);
    }
}