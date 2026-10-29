// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Used to increase the message size")]
public record TestIndexedMessage(int Index, string? Dummy = null) : IIntegrationMessage;
