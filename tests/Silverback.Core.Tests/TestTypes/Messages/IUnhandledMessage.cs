// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.TestTypes.Messages;

public interface IUnhandledMessage
{
    string? Message { get; set; }
}
