// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.SchemaRegistry;
using Newtonsoft.Json.Linq;

namespace Silverback.Messaging.Serialization;

internal static class SchemaNormalizer
{
    public static string Normalize(string schema, SchemaType schemaType) => schemaType switch
    {
        SchemaType.Avro => NormalizeAvro(schema),
        SchemaType.Json => NormalizeJson(schema),
        _ => schema
    };

    public static string NormalizeAvro(string schema) => Avro.Schema.Parse(schema).ToString();

    public static string NormalizeJson(string schema)
    {
        JObject jObject = JObject.Parse(schema);
        SortProperties(jObject);
        return jObject.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static void SortProperties(JObject jObject)
    {
        List<JProperty> properties = jObject.Properties().ToList();
        foreach (JProperty property in properties)
        {
            if (property.Value is JObject value)
                SortProperties(value);

            property.Remove();
        }

        foreach (JProperty property in properties.OrderBy(p => p.Name))
        {
            jObject.Add(property);
        }
    }
}
