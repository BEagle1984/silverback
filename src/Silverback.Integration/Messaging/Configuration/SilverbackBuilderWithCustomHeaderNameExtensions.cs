// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Headers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>WithCustomHeaderName</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderWithCustomHeaderNameExtensions
    {
        /// <summary>
        ///     Adds a new header mapping.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" />.
        /// </param>
        /// <param name="defaultHeaderName">
        ///     The default header name.
        /// </param>
        /// <param name="customHeaderName">
        ///     The custom header name to be used instead of the default.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder WithCustomHeaderName(
            this ISilverbackBuilder silverbackBuilder,
            string defaultHeaderName,
            string customHeaderName)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotEmpty(defaultHeaderName, nameof(defaultHeaderName));
            Check.NotEmpty(customHeaderName, nameof(customHeaderName));

            var mappings = silverbackBuilder.Services.GetSingletonServiceInstance<ICustomHeadersMappings>() ??
                           throw new InvalidOperationException(
                               "ICustomHeadersMappings not found, AddBroker has not been called.");

            mappings.Add(defaultHeaderName, customHeaderName);

            return silverbackBuilder;
        }
    }
}
