// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Examples.Common.Logging;

namespace Silverback.Examples.Consumer
{
    public static class Program
    {
        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "Main method isn't supposed to return")]
        private static async Task Main()
        {
            LoggingConfiguration.Setup();

            await new ConsumerApp().Run();
        }
    }
}
