// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
                _renderer.ShowMenu(_breadcrumbs, GetOptions());
            }
        }

        #region Navigation

        private IMenuItemInfo[] GetOptions()
        {
            IEnumerable<IMenuItemInfo> options =
                _breadcrumbs
                    .Peek()
                    .Children
                    .Select(type => (IMenuItemInfo) Activator.CreateInstance(type))
                    .ToList();

            options = _breadcrumbs.Peek() is RootCategory
                ? options.Append(new ExitMenu())
                : options.Append(new BackMenu());

            return options.ToArray();
        }

        private void OnOptionChosen(object sender, IMenuItemInfo option)
        {
            switch (option)
            {
                case ICategory category:
                    _breadcrumbs.Push(category);
                    break;
                case IUseCase useCase:
                    RunUseCase(useCase);
                    break;
                case BackMenu _:
                case ExitMenu _:
                    OnBack(sender, EventArgs.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnBack(object sender, EventArgs args)
        {
            _breadcrumbs.Pop();

            if (!_breadcrumbs.Any())
                _exiting = true;
        }

        #endregion

        #region Run UseCase

        private void RunUseCase(IUseCase useCase)
        {
            Console.Clear();
            WriteUseCaseHeader(useCase);

            useCase.Run();

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.Write("\r\nPress any key to continue...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        private void WriteUseCaseHeader(IMenuItemInfo useCase) =>
            _breadcrumbs.Prepend(useCase).WriteBreadcrumbs();

        #endregion
    }
}