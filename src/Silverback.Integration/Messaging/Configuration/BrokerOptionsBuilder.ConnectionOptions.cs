// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <see cref="WithConnectionOptions" /> and related methods.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Specifies the <see cref="BrokerClientConnectionOptions" />.
    /// </summary>
    /// <param name="clientConnectionOptions">
    ///     The <see cref="BrokerClientConnectionOptions" /> to apply.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder WithConnectionOptions(BrokerClientConnectionOptions clientConnectionOptions)
    {
        SilverbackBuilder.Services.RemoveAll<BrokerClientConnectionOptions>();
        SilverbackBuilder.Services.AddSingleton(clientConnectionOptions);
        return this;
    }

    /// <summary>
    ///     Specifies that the broker clients have to be connected during the application startup.
    /// </summary>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder ConnectAtStartup()
    {
        GetCurrentOptions().Mode = BrokerClientConnectionMode.Startup;
        return this;
    }

    /// <summary>
    ///     Specifies that the broker clients have to be connected after the application is successfully started.
    /// </summary>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder ConnectAfterStartup()
    {
        GetCurrentOptions().Mode = BrokerClientConnectionMode.AfterStartup;
        return this;
    }

    /// <summary>
    ///     Specifies that the broker clients must be initialized but not connected. They can be manually connected later.
    /// </summary>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder ManuallyConnect()
    {
        GetCurrentOptions().Mode = BrokerClientConnectionMode.Manual;
        return this;
    }

    /// <summary>
    ///     Specifies that a retry must be performed if an exception is thrown when trying to connect.
    /// </summary>
    /// <param name="retryInterval">
    ///     The interval between the connection retries. The default is 5 minutes.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder RetryOnConnectionFailure(TimeSpan? retryInterval = null)
    {
        GetCurrentOptions().RetryOnFailure = true;

        if (retryInterval.HasValue)
            GetCurrentOptions().RetryInterval = retryInterval.Value;

        return this;
    }

    /// <summary>
    ///     Specifies that no retry must be performed if an exception is thrown when trying to connect.
    /// </summary>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder DisableRetryOnConnectionFailure()
    {
        GetCurrentOptions().RetryOnFailure = false;
        return this;
    }

    private BrokerClientConnectionOptions GetCurrentOptions() =>
        SilverbackBuilder.Services.GetSingletonServiceInstance<BrokerClientConnectionOptions>() ??
        throw new InvalidOperationException("BrokerClientConnectionOptions not found, WithConnectionTo has not been called.");
}
