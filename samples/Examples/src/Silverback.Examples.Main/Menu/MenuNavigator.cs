// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Examples.Main.UseCases;

namespace Silverback.Examples.Main.Menu
{
    public class MenuNavigator
    {
        public void RenderCategories()
        {
            while (true)
            {
                var categories = MenuItem.GetAll<UseCaseCategory>();

                ShowSplash();
                Console.WriteLine("Categories:");
                for (var i = 0; i < categories.Length; i++)
                {
                    Console.WriteLine($" {i+1}. {categories[i].Name}");
                }
                Console.WriteLine(" Q. Quit");
                Console.WriteLine();
                Console.Write("? ");

                var input = Console.ReadLine();

                if (input.ToLower() == "q")
                    return;

                if (int.TryParse(input, out var categoryIndex)
                    && categoryIndex >= 1 && categoryIndex <= categories.Length)
                {
                    RenderUseCases(categories[categoryIndex - 1]);
                }
            }
        }

        public void RenderUseCases(UseCaseCategory category)
        {
            while (true)
            {
                var useCases = category.GetUseCases();

                ShowSplash();
                Console.WriteLine($"Use cases in category '{category.Name}':");
                for (var i = 0; i < useCases.Length; i++)
                {
                    Console.WriteLine($" {i+1}. {useCases[i].Name}");
                }

                Console.WriteLine(" Q. Quit to categories list");
                Console.WriteLine();
                Console.Write("? ");

                var input = Console.ReadLine();

                if (input.ToLower() == "q")
                    return;

                if (int.TryParse(input, out var useCaseIndex)
                    && useCaseIndex >= 1 && useCaseIndex <= useCases.Length)
                {
                    Console.Clear();

                    useCases[useCaseIndex - 1].Execute();

                    Console.ReadLine();
                }
            }
        }

        private void ShowSplash()
        {
            Console.Clear();
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
            Console.WriteLine();

            Console.ResetColor();
        }
    }
}
