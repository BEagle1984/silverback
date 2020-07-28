// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Examples.Common;
using Silverback.Examples.Main.UseCases;

namespace Silverback.Examples.Main.Menu
{
    public class MenuApp
    {
        private readonly MenuRenderer _renderer = new MenuRenderer();

        private readonly Stack<ICategory> _breadcrumbs = new Stack<ICategory>();

        private bool _exiting;

        private IMenuItemInfo? _preSelectedItem;

        public MenuApp()
        {
            _renderer.Chosen += OnOptionChosen;
            _renderer.Back += OnBack;

            _breadcrumbs.Push(new RootCategory());
        }

        public void Run()
        {
            while (!_exiting)
            {
                _renderer.ShowMenu(_breadcrumbs, GetOptions(), _preSelectedItem);
            }
        }

        [SuppressMessage("ReSharper", "SA1009", Justification = "False positive")]
        private IMenuItemInfo[] GetOptions()
        {
            IEnumerable<IMenuItemInfo> options =
                _breadcrumbs
                    .Peek()
                    .Children
                    .Select(type => (IMenuItemInfo)Activator.CreateInstance(type)!)
                    .ToList();

            options = _breadcrumbs.Peek() is RootCategory
                ? options.Append(new ExitMenu())
                : options.Append(new BackMenu());

            return options.ToArray();
        }

        private void OnOptionChosen(object? sender, MenuItemInfoEventArgs args)
        {
            switch (args.MenuItemInfo)
            {
                case ICategory category:
                    _breadcrumbs.Push(category);
                    break;
                case IUseCase useCase:
                    _preSelectedItem = useCase;
                    RunUseCase(useCase);
                    break;
                case BackMenu _:
                case ExitMenu _:
                    OnBack(sender, EventArgs.Empty);
                    break;
                default:
                    throw new InvalidOperationException("Invalid menu item type.");
            }
        }

        private void OnBack(object? sender, EventArgs args)
        {
            _preSelectedItem = _breadcrumbs.Pop();

            if (!_breadcrumbs.Any())
                _exiting = true;
        }

        private void RunUseCase(IUseCase useCase)
        {
            Console.Clear();
            WriteUseCaseHeader(useCase);

            useCase.Run();

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.Write("\r\nPress any key to continue...");
            ConsoleHelper.ResetColor();
            Console.ReadKey(true);
        }

        private void WriteUseCaseHeader(IMenuItemInfo useCase) =>
            _breadcrumbs.Prepend(useCase).WriteBreadcrumbs();
    }
}
