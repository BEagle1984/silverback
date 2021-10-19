// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging
{
    internal static class EndpointConfigurationBuilderIsValidExtensions
    {
        // TODO: TEST??




        public static TConfiguration? BuildAndValidate<TMessage, TConfiguration, TBuilder>(this EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder> builder, Action<TBuilder> builderAction, ISilverbackLogger logger)
            where TConfiguration : EndpointConfiguration
            where TBuilder : EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
        {
            TConfiguration? endpointConfiguration;

            try
            {
                builderAction.Invoke((TBuilder)builder);
                endpointConfiguration = builder.Build();
            }
            catch (Exception ex)
            {
                logger.LogEndpointBuilderError(builder.EndpointDisplayName, ex);
                return null;
            }

            try
            {
                endpointConfiguration.Validate();
            }
            catch (Exception ex)
            {
                logger.LogInvalidEndpointConfiguration(endpointConfiguration, ex);
                return null;
            }

            return endpointConfiguration;
        }

        /// <summary>
        ///     Validates the endpoint configuration and logs a critical if the configuration is not valid.
        /// </summary>
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public static bool IsValid(this EndpointConfiguration endpointConfiguration, ISilverbackLogger logger)
        {
            Check.NotNull(logger, nameof(logger));

            try
            {
                Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));

                endpointConfiguration.Validate();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogInvalidEndpointConfiguration(endpointConfiguration, ex);
                return false;
            }
        }
    }
}
