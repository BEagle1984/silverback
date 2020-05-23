// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Core.TestTypes.Messages.Base
{
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by the Publisher")]
    public interface ICommand<out TResult> : ICommand
    {
    }
}
