// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Specifies if and when the message broker has to be automatically connected.
/// </summary>
public enum BrokerClientConnectionMode
{
    /// <summary>
    ///     The broker clients are being connected during the application startup.
    /// </summary>
    Startup,

    /// <summary>
    ///     The broker clients are being connected after the application is successfully started.
    /// </summary>
    AfterStartup,

    /// <summary>
    ///     The broker clients are not being connected automatically.
    /// </summary>
    Manual
}
