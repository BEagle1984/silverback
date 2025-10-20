// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

internal interface ITestInboundEnvelope : IInboundEnvelope
{
    object? Key { get; init; }
}
