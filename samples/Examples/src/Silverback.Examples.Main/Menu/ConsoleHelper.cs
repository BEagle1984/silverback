// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Examples.Common;

namespace Silverback.Examples.Main.Menu
{
    public static class ConsoleHelper
    {
        public static int ConsoleWidth => Math.Min(100, Console.WindowWidth - 2);

        public static void WriteSeparator(ConsoleColor? color = null)
        {
            Console.ForegroundColor = color ?? Constants.SecondaryColor;
            Console.WriteLine("".PadRight(ConsoleWidth, '-'));
            Console.ResetColor();
        }

        public static void WriteLineTruncated(string text, int? maxWidth = null)
        {
            maxWidth ??= ConsoleWidth;
            
            Console.WriteLine(text.Substring(0, Math.Min(maxWidth.Value, text.Length)));
        }
        
        public static void WriteLineWrapped(string text, int? maxWidth = null)
        {
            maxWidth ??= ConsoleWidth;
            
            while (text.Length > 0)
            {
                var line = text.Substring(0, Math.Min(maxWidth.Value, text.Length));
                Console.WriteLine(line.TrimStart());
                text = text.Substring(line.Length);
            }
        }
        
        public static void WriteLineBoxed(
            string text,
            char boxChar = '¦',
            int? width = null, 
            ConsoleColor? boxColor = null,
            ConsoleColor? textColor = null)
        {
            width ??= ConsoleWidth - 4;
            boxColor ??= Constants.SecondaryColor;
            
            while (text.Length > 0)
            {
                var line = text.Substring(0, Math.Min(width.Value, text.Length));

                Console.ForegroundColor = boxColor.Value;
                Console.Write($"{boxChar} ");
                
                if (textColor.HasValue)
                    Console.ForegroundColor = textColor.Value;
                else
                    Console.ResetColor();
                
                Console.Write(line.TrimStart().PadRight(width.Value));
                
                Console.ForegroundColor = boxColor.Value;
                Console.WriteLine($" {boxChar}");
                
                text = text.Substring(line.Length);
            }
            Console.ResetColor();
        }
    }
}