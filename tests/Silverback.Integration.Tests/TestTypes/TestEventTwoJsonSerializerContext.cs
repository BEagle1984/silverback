// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json.Serialization;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Integration.TestTypes;

[JsonSerializable(typeof(TestEventTwo))]
public partial class TestEventTwoJsonSerializerContext : JsonSerializerContext;
