// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging
{
    internal static class EndpointIsValidExtensions
    {
        /// <summary>
        ///     Validates the endpoint configuration and logs a critical if the configuration is not valid.
        /// </summary>
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public static bool IsValid(this IEndpoint endpoint, ISilverbackLogger logger)
        {
            Check.NotNull(logger, nameof(logger));

            try
            {
                Check.NotNull(endpoint, nameof(endpoint));

                endpoint.Validate();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogInvalidEndpointConfiguration(endpoint, ex);
                return false;
            }
        }
    }
}
