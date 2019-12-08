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

                var selected = MenuHelper.Choice("Choose a category:",
                    categories
                        .Select((category, i) => $"{i+1}. {category.Name}")
                        .Append("<- Exit")
                        .ToArray());

                if (selected < 0|| selected >= categories.Length)
                    return;

                RenderUseCases(categories[selected]);
            }
        }

        public void RenderUseCases(UseCaseCategory category)
        {
            var useCases = category.GetUseCases();

            while (true)
            {
                var selected = MenuHelper.Choice(
                    () =>
                    {
                        Console.Write("Choose a use cases in category '");
                        Console.ForegroundColor = Constants.PrimaryColor;
                        Console.Write(category.Name);
                        Console.ResetColor();
                        Console.WriteLine("':");
                        Console.WriteLine();
                    },
                    useCases
                        .Select((useCase, i) => $"{i + 1}. {useCase.Name}")
                        .Append("<- Back")
                        .ToArray());

                if (selected < 0 || selected >= useCases.Length)
                    return;

                Console.Clear();
                useCases[selected].Execute();
                Console.ForegroundColor = Constants.PrimaryColor;
                Console.Write("\r\nPress any key to continue...");
                Console.ResetColor();
                Console.ReadLine();
            }
        }
    }
}
