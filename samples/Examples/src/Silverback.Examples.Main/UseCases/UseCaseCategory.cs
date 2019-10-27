// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases
{
    public abstract class UseCaseCategory : MenuItem
    {
        protected UseCaseCategory(string name, int sortIndex = 100)
            : base(name, sortIndex)
        {
        }

        public UseCase[] GetUseCases() => GetChildren<UseCase>();
    }
}
