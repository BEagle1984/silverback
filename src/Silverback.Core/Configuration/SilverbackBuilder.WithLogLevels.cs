// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Implements the <c>WithLogLevels</c> methods.
/// </content>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Configures the log levels that should be used to log the standard Silverback events.
    /// </summary>
    /// <param name="logLevelsConfigurationAction">
    ///     The log levels configuration action.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder WithLogLevels(Action<LogLevelConfigurator> logLevelsConfigurationAction)
    {
        Check.NotNull(logLevelsConfigurationAction, nameof(logLevelsConfigurationAction));

        LogLevelConfigurator configurator = new();
        logLevelsConfigurationAction.Invoke(configurator);
        Services.Replace(ServiceDescriptor.Singleton(configurator.LogLevelDictionary));

        return this;
    }
}
