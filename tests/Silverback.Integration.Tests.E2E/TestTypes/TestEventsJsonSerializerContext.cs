// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes;

[JsonSerializable(typeof(TestEventOne))]
[JsonSerializable(typeof(TestEventTwo))]
public partial class TestEventsJsonSerializerContext : JsonSerializerContext;
