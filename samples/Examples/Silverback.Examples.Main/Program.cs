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
            Console.Clear();

            ShowSplash();

            new MenuNavigator().RenderCategories();
        }

        static void ShowSplash()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(@" _____ _ _                _                _      _____                          _");
            Console.WriteLine(@"/  ___(_) |              | |              | |    |  ___|                        | |");
            Console.WriteLine(@"\ `--. _| |_   _____ _ __| |__   __ _  ___| | __ | |____  ____ _ _ __ ___  _ __ | | ___  ___");
            Console.WriteLine(@" `--. \ | \ \ / / _ \ '__| '_ \ / _` |/ __| |/ / |  __\ \/ / _` | '_ ` _ \| '_ \| |/ _ \/ __|");
            Console.WriteLine(@"/\__/ / | |\ V /  __/ |  | |_) | (_| | (__|   < _| |___>  < (_| | | | | | | |_) | |  __/\__ \");
            Console.WriteLine(@"\____/|_|_| \_/ \___|_|  |_.__/ \__,_|\___|_|\_(_)____/_/\_\__,_|_| |_| |_| .__/|_|\___||___/");
            Console.WriteLine(@"                                                                          | |                ");
            Console.WriteLine(@"                                                                          |_|                ");
            Console.WriteLine();

            Console.ResetColor();
        }
    }
}
