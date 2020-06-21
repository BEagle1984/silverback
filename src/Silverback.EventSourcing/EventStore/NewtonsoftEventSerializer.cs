// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
using Silverback.Domain;

namespace Silverback.EventStore
{
    [Obsolete("Replaced by new EventSerializer using System.Text.Json")]
    internal static class NewtonsoftEventSerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static IEntityEvent Deserialize(string json)
        {
            var deserialized = JsonConvert.DeserializeObject<IEntityEvent>(json, SerializerSettings);

            if (deserialized == null)
                throw new EventStoreSerializationException("The deserialized event is null.");

            return deserialized;
        }
    }
}
