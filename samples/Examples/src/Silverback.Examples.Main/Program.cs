// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main
{
    static class Program
    {
        static void Main()
        {
            new MenuNavigator().RenderCategories();
            
            Console.Clear();
        }
    }
}
