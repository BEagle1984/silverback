// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.UI;

public static class ConsoleHelper
{
    public static void WriteDone()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("done");
        Console.ResetColor();
    }

    public static void WriteDone(TimeSpan elapsed)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("done");
        Console.ResetColor();
        Console.WriteLine($" ({elapsed.TotalSeconds:0.00}s)");
    }
}
