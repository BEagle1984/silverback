// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Silverback.Serialization
{
    /// <summary>
    ///     Wraps the <see cref="JsonSerializer" /> handling the <c>$type</c> property as defined by
    ///     Newtonsoft.Json. This is used merely for backward compatibility with the serialized data stored in the
    ///     database.
    /// </summary>
    internal static class PolymorphicJsonSerializer
    {
        private const string TypePropertyName = "$type";

        public static object? Deserialize(string json, JsonSerializerOptions? options = null)
        {
            var typeInformation = JsonSerializer.Deserialize<TypeInformation>(json, options);

            if (string.IsNullOrEmpty(typeInformation?.TypeName))
                throw new JsonException($"No {TypePropertyName} property found in JSON.");

            var type = TypesCache.GetType(typeInformation.TypeName);

            return JsonSerializer.Deserialize(json, type, options);
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Used in Deserialize method")]
        private class TypeInformation
        {
            [JsonPropertyName(TypePropertyName)]
            public string? TypeName { get; set; }
        }
    }
}
