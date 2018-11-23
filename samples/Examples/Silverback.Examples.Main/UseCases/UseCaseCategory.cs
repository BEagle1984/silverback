using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
