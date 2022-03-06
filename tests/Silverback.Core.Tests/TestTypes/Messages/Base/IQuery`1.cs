﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.TestTypes.Messages.Base;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by the Publisher")]
public interface IQuery<out TResult> : IMessage
{
}
