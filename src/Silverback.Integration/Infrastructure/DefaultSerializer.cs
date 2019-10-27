// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;

namespace Silverback.Infrastructure
{
    internal static class DefaultSerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, typeof(T), SerializerSettings);

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SerializerSettings);
    }
}