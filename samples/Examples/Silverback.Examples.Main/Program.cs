using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main
{
    class Program
    {
        static void Main(string[] args)
        {
            new MenuNavigator().RenderCategories();
        }
    }
}
