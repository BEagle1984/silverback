// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Examples.Main.Menu
{
public class MenuHelper
{
    public static int Choice(string title, params string[] options) =>
        Choice(() => Console.WriteLine(title + Environment.NewLine), options);

    public static int Choice(Action headerAction, params string[] options)
    {
        var selected = 0;

        ConsoleKey key;

        Console.CursorVisible = false;

        do
        {
            Console.Clear();
            ShowSplash();

            headerAction?.Invoke();

            for (var i = 0; i < options.Length; i++)
            {
                if (i == selected)
                {
                    Console.BackgroundColor = Constants.PrimaryColor;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.WriteLine($" {options[i]} ");

                Console.ResetColor();
            }

            key = Console.ReadKey(true).Key;

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
                    return -1;
                }
            }
        } while (key != ConsoleKey.Enter);

        Console.CursorVisible = true;

        return selected;
    }
    private static void ShowSplash()
    {
        Console.Clear();
        Console.ForegroundColor = Constants.PrimaryColor;

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
    }}
}