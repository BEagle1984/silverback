// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Silverback.Examples.ConsumerA
{
    static class Program
    {
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        static void Main()
        {
            new ConsumerServiceA().Init();

            while (true)
            {
                Thread.Sleep(5000);
            }
        }
    }
}
