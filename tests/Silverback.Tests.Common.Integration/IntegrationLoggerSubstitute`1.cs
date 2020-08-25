// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;

namespace Silverback.Tests
{
    internal class IntegrationLoggerSubstitute<TCategoryName> : SilverbackIntegrationLogger<TCategoryName>
    {
        public IntegrationLoggerSubstitute()
            : base(new LoggerSubstitute<TCategoryName>(), new LogTemplates())
        {
        }
    }
}
