// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Headers;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the WithCustomHeaderName method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Adds a new header mapping.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" />.
    /// </param>
    /// <param name="defaultHeaderName">
    ///     The default header name. Must not be empty.
    /// </param>
    /// <param name="customHeaderName">
    ///     The custom header name to be used instead of the default. Must not be empty.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     The broker integration hasn't been added, because <c>WithConnectionToMessageBroker</c> hasn't been called.
    /// </exception>
    public static SilverbackBuilder WithCustomHeaderName(this SilverbackBuilder builder, string defaultHeaderName, string customHeaderName)
    {
        Check.NotNull(builder);
        Check.NotNullOrEmpty(defaultHeaderName);
        Check.NotNullOrEmpty(customHeaderName);

        ICustomHeadersMappings mappings = builder.Services.GetSingletonServiceInstance<ICustomHeadersMappings>() ??
                                          throw new InvalidOperationException("ICustomHeadersMappings not found, WithConnectionToMessageBroker has not been called.");

        mappings.Add(defaultHeaderName, customHeaderName);

        return builder;
    }
}
