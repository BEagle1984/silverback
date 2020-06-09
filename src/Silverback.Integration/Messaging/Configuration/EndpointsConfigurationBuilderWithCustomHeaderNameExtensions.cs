// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Headers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>WithCustomHeaderName</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderWithCustomHeaderNameExtensions
    {
        /// <summary>
        ///     Adds a new header mapping.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="defaultHeaderName">
        ///     The default header name.
        /// </param>
        /// <param name="customHeaderName">
        ///     The custom header name to be used instead of the default.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder WithCustomHeaderName(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            string defaultHeaderName,
            string customHeaderName)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotEmpty(defaultHeaderName, nameof(defaultHeaderName));
            Check.NotEmpty(customHeaderName, nameof(customHeaderName));

            var mappings = endpointsConfigurationBuilder.ServiceProvider.GetRequiredService<ICustomHeadersMappings>();

            mappings.Add(defaultHeaderName, customHeaderName);

            return endpointsConfigurationBuilder;
        }
    }
}
