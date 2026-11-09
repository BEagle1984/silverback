// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Avro;
using Avro.Specific;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka.SchemaRegistry;

// Hand-written equivalent of the avrogen output. The Avro reader instantiates the record by the full name of its schema, so the
// schema namespace and name must match the class.
public class AvroTestMessage : ISpecificRecord
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by the Avro serializer")]
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "AvroTestMessage",
          "namespace": "Silverback.Tests.Extended.Integration.TestHost.Kafka.SchemaRegistry",
          "fields": [{ "name": "number", "type": "string" }]
        }
        """);

    public virtual Schema Schema => _SCHEMA;

    public string Number { get; set; } = string.Empty;

    public virtual object Get(int fieldPos) => fieldPos switch
    {
        0 => Number,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public virtual void Put(int fieldPos, object fieldValue) =>
        Number = fieldPos switch
        {
            0 => (string)fieldValue,
            _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Put()")
        };
}
