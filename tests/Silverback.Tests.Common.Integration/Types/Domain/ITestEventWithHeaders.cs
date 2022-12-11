// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types.Domain;

public interface ITestEventWithHeaders
{
    [Header("x-inherited")]
    string? InheritedHeader { get; set; }
}
