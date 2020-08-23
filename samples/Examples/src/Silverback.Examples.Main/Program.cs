// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Examples.Common.Logging;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main
{
    public static class Program
    {
        public static void Main()
        {
            LoggingConfiguration.Setup();

            Console.Clear();
            Console.CursorVisible = false;

            new MenuApp().Run();

            Console.Clear();
        }
    }
}
