// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Examples.Common;
using Silverback.Examples.Main.UseCases;

namespace Silverback.Examples.Main.Menu
{
    public static class BreadcrumbsHelper
    {
        public static bool IsRoot(this IEnumerable<IMenuItemInfo> breadcrumbs) =>
            breadcrumbs.All(item => item is RootCategory);

        public static void WriteBreadcrumbs(this IEnumerable<IMenuItemInfo> breadcrumbs)
        {
            var items = breadcrumbs.Reverse().ToArray();

            if (items.IsRoot())
                return;

            var isEmpty = true;

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Title))
                    continue;

                if (!isEmpty)
                {
                    Console.ForegroundColor = Constants.SecondaryColor;
                    Console.Write(" > ");
                }

                Console.ForegroundColor = item == items.Last()
                    ? Constants.PrimaryColor
                    : Constants.SecondaryColor;

                Console.Write($"{item.Title}");

                isEmpty = false;
            }

            Console.WriteLine();
            Console.WriteLine();

            ConsoleHelper.ResetColor();
        }
    }
}