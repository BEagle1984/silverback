// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;
using Silverback.Domain;

namespace Silverback.EventStore
{
    internal static class EventSerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static string Serialize(IEntityEvent @event) =>
            JsonConvert.SerializeObject(@event, typeof(IEntityEvent), SerializerSettings);

        public static IEntityEvent Deserialize(string json)
        {
            var deserialized = JsonConvert.DeserializeObject<IEntityEvent>(json, SerializerSettings);

            if (deserialized == null)
                throw new EventStoreSerializationException("The deserialized event is null.");

            return deserialized;
        }
    }
}
