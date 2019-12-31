// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Serilog;
using Serilog.Configuration;

namespace Silverback.Examples.Common.Logging
{
    public static class LoggerEnrichmentConfigurationExtensions
    {
        public static LoggerConfiguration WithActivityId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration.With<ActivityIdEnricher>();
        }
    }
}