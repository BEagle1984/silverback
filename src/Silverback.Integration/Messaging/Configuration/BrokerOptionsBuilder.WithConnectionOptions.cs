// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the WithConnectionOptions method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Specifies the <see cref="BrokerConnectionOptions" />.
    /// </summary>
    /// <param name="connectionOptions">
    ///     The <see cref="BrokerConnectionOptions" /> to apply.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder WithConnectionOptions(BrokerConnectionOptions connectionOptions)
    {
        SilverbackBuilder.Services.RemoveAll<BrokerConnectionOptions>();
        SilverbackBuilder.Services.AddSingleton(connectionOptions);
        return this;
    }
}
