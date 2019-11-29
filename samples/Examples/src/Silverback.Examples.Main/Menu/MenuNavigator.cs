// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
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

                var selected = MenuHelper.Choice("Choose a category (or press ESC to exit):",
                    categories.Select((category, i) => $"{i+1}. {category.Name}").ToArray());

                if (selected < 0)
                    return;

                RenderUseCases(categories[selected]);
            }
        }

        public void RenderUseCases(UseCaseCategory category)
        {
            var useCases = category.GetUseCases();

            while (true)
            {
                var selected = MenuHelper.Choice($"Choose a use cases in category '{category.Name}' (or press ESC to return to the categories list):",
                    useCases.Select((useCase, i) => $"{i+1}. {useCase.Name}").ToArray());

                if (selected < 0)
                    return;

                Console.Clear();
                useCases[selected].Execute();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\r\nPress any key to continue...");
                Console.ResetColor();
                Console.ReadLine();
            }
        }
    }
}
