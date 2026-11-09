// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka.SchemaRegistry;

// The Confluent serializer generates the JSON schema from this type
public class JsonTestMessage
{
    public string? Number { get; set; }
}
