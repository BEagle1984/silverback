// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.EFCore22.TestTypes.Base
{
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface ICommand : IMessage
    {
    }
}