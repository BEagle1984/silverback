// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Examples.Consumer
{
    public static class Program
    {
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        static void Main()
        {
            new ConsumerApp().Start();
        }
    }
}