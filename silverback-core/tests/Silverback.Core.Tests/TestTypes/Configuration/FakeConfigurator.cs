using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.TestTypes.Configuration
{
    public class FakeConfigurator : IConfigurator
    {
        public static bool Executed { get; set; }

        public void Configure(BusConfig config)
        {
            if (config == null) throw new ArgumentNullException();

            Executed = true;
        }
    }
}
