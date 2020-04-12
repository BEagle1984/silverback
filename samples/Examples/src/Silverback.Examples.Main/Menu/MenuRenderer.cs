// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Examples.Common;

namespace Silverback.Examples.Main.Menu
{
    public class MenuRenderer
    {
        public event EventHandler<IMenuItemInfo> Chosen;
        public event EventHandler Back;

        public void ShowMenu(
            IReadOnlyCollection<IMenuItemInfo> breadcrumbs,
            IMenuItemInfo[] options,
            IMenuItemInfo selectedItem = null)
        {
            var selectedIndex = selectedItem != null
                ? GetIndex(options, selectedItem)
                : GetFirstCategoryOrUseCaseIndex(options);

            do
            {
                Console.Clear();
                ConsoleHelper.ResetColor();

                WriteHeader(breadcrumbs);
                WriteOptions(options, selectedIndex);

                WriteSelectionDescription(options[selectedIndex]);

                HandleInput(options, ref selectedIndex);
            } while (selectedIndex != -1);
        }

        private int GetIndex(IMenuItemInfo[] options, IMenuItemInfo selectedItem)
        {
            var selectedOption = options.FirstOrDefault(item => item.GetType() == selectedItem.GetType());
            return selectedOption != null ? Array.IndexOf(options, selectedOption) : 0;
        }
        
        private int GetFirstCategoryOrUseCaseIndex(IMenuItemInfo[] options)
        {
            var firstItem = options.FirstOrDefault(item => !(item is IBackMenu));
            return firstItem != null ? Array.IndexOf(options, firstItem) : 0;
        }

        private static void WriteOptions(IMenuItemInfo[] options, int selectedIndex)
        {
            for (var i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.ForegroundColor = Constants.AccentColor;
                    WriteSelectedGlyph(options[i]);
                }
                else
                {
                    Console.ForegroundColor = Constants.SecondaryColor;

                    switch (options[i])
                    {
                        case ICategory _:
                        case IUseCase _:
                            Console.Write($"{i + 1}.".PadLeft(3) + " ");
                            break;
                        case IBackMenu _:
                            Console.Write(" <  ");
                            break;
                    }
                }

                if (i == selectedIndex)
                    Console.ForegroundColor = Constants.PrimaryColor;
                else
                    ConsoleHelper.ResetColor();

                Console.WriteLine($"{options[i].Title} ");

                ConsoleHelper.ResetColor();
            }
        }

        private static void WriteSelectedGlyph(IMenuItemInfo option)
        {
            switch (option)
            {
                case BackMenu _:
                    Console.Write("<-  ");
                    break;
                case ExitMenu _:
                    Console.Write("<-  ");
                    break;
                case ICategory _:
                    Console.Write("->  ");
                    break;
                case IUseCase _:
                    Console.Write(">>  ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleInput(IMenuItemInfo[] options, ref int selected)
        {
            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                {
                    if (selected <= 0)
                        selected = options.GetUpperBound(0);
                    else
                        selected--;
                    break;
                }
                case ConsoleKey.DownArrow:
                {
                    if (selected >= options.GetUpperBound(0))
                        selected = 0;
                    else
                        selected++;
                    break;
                }
                case ConsoleKey.Backspace:
                case ConsoleKey.Escape:
                {
                    Back?.Invoke(this, EventArgs.Empty);
                    selected = -1;
                    break;
                }
                case ConsoleKey.Enter:
                {
                    Chosen?.Invoke(this, options[selected]);
                    selected = -1;
                    break;
                }
            }
        }

        private void WriteSelectionDescription(IMenuItemInfo option)
        {
            if (string.IsNullOrEmpty(option.Description))
                return;

            Console.WriteLine();
            Console.ForegroundColor = Constants.SecondaryColor;
            Console.WriteLine($"{option.Description}");

            if (!(option is IBackMenu))
            {
                Console.WriteLine(option is IUseCase
                    ? "(press ENTER to run the use case)"
                    : "(press ENTER to explore the category)");
            }
        }

        private static void WriteHeader(IReadOnlyCollection<IMenuItemInfo> breadcrumbs)
        {
            if (breadcrumbs.IsRoot())
            {
                Console.ForegroundColor = Constants.PrimaryColor;

                ConsoleHelper.WriteLineTruncated(@" ____ ___ _ __     _______ ____  ____    _    ____ _  __");
                ConsoleHelper.WriteLineTruncated(@"/ ___|_ _| |\ \   / / ____|  _ \| __ )  / \  / ___| |/ /");
                ConsoleHelper.WriteLineTruncated(@"\___ \| || | \ \ / /|  _| | |_) |  _ \ / _ \| |   | ' / ");
                ConsoleHelper.WriteLineTruncated(@" ___) | || |__\ V / | |___|  _ <| |_) / ___ \ |___| . \ ");
                ConsoleHelper.WriteLineTruncated(@"|____/___|_____\_/  |_____|_| \_\____/_/   \_\____|_|\_\");
                ConsoleHelper.WriteLineTruncated(GetSilverbackVersion().PadLeft(56));

                Console.WriteLine();
            }

            breadcrumbs.WriteBreadcrumbs();

            ConsoleHelper.ResetColor();
        }

        private static string GetSilverbackVersion() =>
            Assembly.Load("Silverback.Core").GetName().Version.ToString(3);
    }
}