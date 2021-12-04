// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Specifies if and when the message broker has to be automatically connected.
/// </summary>
public enum BrokerConnectionMode
{
    /// <summary>
    ///     The message broker is being connected during the application startup.
    /// </summary>
    Startup,

    /// <summary>
    ///     The message broker is being connected after the application is successfully started.
    /// </summary>
    AfterStartup,

    /// <summary>
    ///     The message broker is not being connected automatically.
    /// </summary>
    Manual
}
