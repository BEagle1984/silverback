// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Diagnostics
{
    internal class SilverbackIntegrationLogger<TCategoryName>
        : SilverbackIntegrationLogger, ISilverbackIntegrationLogger<TCategoryName>
    {
        public SilverbackIntegrationLogger(ISilverbackLogger<TCategoryName> logger, ILogTemplates logTemplates)
            : base(logger, logTemplates)
        {
        }
    }
}
