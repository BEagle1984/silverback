// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Examples.Common;

namespace Silverback.Examples.Main.Menu
{
    public class MenuRenderer
    {
        public event EventHandler<IMenuItemInfo> Chosen;
        public event EventHandler Back;

        public void ShowMenu(IEnumerable<IMenuItemInfo> breadcrumbs, IMenuItemInfo[] options)
        {
            var selected = GetFirstCategoryOrUseCaseIndex(options);

            do
            {
                Console.Clear();

                WriteHeader(breadcrumbs);
                WriteOptions(options, selected);

                WriteSelectionDescription(options[selected]);

                HandleInput(options, ref selected);
            } while (selected != -1);
        }

        private int GetFirstCategoryOrUseCaseIndex(IMenuItemInfo[] options)
        {
            var firstItem = options.FirstOrDefault(item => !(item is IBackMenu));
            return firstItem != null ? Array.IndexOf(options, firstItem) : 0;
        }

        private static void WriteOptions(IMenuItemInfo[] options, int selected)
        {
            for (var i = 0; i < options.Length; i++)
            {
                if (i == selected)
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
                            Console.Write($"{i + 1}. ");
                            break;
                        case IBackMenu _:
                            Console.Write("<  ");
                            break;
                    }
                }

                if (i == selected)
                    Console.ForegroundColor = Constants.PrimaryColor;
                else
                    Console.ResetColor();

                Console.WriteLine($"{options[i].Title} ");

                Console.ResetColor();
            }
        }

        private static void WriteSelectedGlyph(IMenuItemInfo option)
        {
            switch (option)
            {
                case BackMenu _:
                    Console.Write("<- ");
                    break;
                case ExitMenu _:
                    Console.Write("<- ");
                    break;
                case ICategory _:
                    Console.Write("-> ");
                    break;
                case IUseCase _:
                    Console.Write(">> ");
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

        private static void WriteHeader(IEnumerable<IMenuItemInfo> breadcrumbs)
        {
            if (breadcrumbs.IsRoot())
            {
                Console.ForegroundColor = Constants.PrimaryColor;
                ConsoleHelper.WriteLineTruncated(
                    @" _____ _ _                _                _      _____                          _");
                ConsoleHelper.WriteLineTruncated(
                    @"/  ___(_) |              | |              | |    |  ___|                        | |");
                ConsoleHelper.WriteLineTruncated(
                    @"\ `--. _| |_   _____ _ __| |__   __ _  ___| | __ | |____  ____ _ _ __ ___  _ __ | | ___  ___");
                ConsoleHelper.WriteLineTruncated(
                    @" `--. \ | \ \ / / _ \ '__| '_ \ / _` |/ __| |/ / |  __\ \/ / _` | '_ ` _ \| '_ \| |/ _ \/ __|");
                ConsoleHelper.WriteLineTruncated(
                    @"/\__/ / | |\ V /  __/ |  | |_) | (_| | (__|   < _| |___>  < (_| | | | | | | |_) | |  __/\__ \");
                ConsoleHelper.WriteLineTruncated(
                    @"\____/|_|_| \_/ \___|_|  |_.__/ \__,_|\___|_|\_(_)____/_/\_\__,_|_| |_| |_| .__/|_|\___||___/");
                ConsoleHelper.WriteLineTruncated(
                    @"                                                                          | |                ");
                ConsoleHelper.WriteLineTruncated(
                    @"                                                                          |_|                ");
                Console.ResetColor();
                Console.WriteLine();
            }

            breadcrumbs.WriteBreadcrumbs();
        }
    }
}