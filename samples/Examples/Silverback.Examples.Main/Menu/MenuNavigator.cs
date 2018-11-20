using System;
using System.Collections.Generic;
using System.Text;
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

                Console.WriteLine();
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
                    Console.Clear();
                    RenderUseCases(categories[categoryIndex - 1]);
                }

                Console.Clear();
            }
        }

        public void RenderUseCases(UseCaseCategory category)
        {
            while (true)
            {
                var useCases = category.GetUseCases();

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

                Console.Clear();
            }
        }
    }
}
