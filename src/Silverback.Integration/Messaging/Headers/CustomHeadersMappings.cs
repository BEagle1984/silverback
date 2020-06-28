// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers
{
    internal class CustomHeadersMappings : ICustomHeadersMappings
    {
        private Dictionary<string, string>? _mappings;

        private Dictionary<string, string>? _inverseMappings;

        public int Count { get; private set; }

        public void Add(string defaultHeaderName, string customHeaderName)
        {
            _mappings ??= new Dictionary<string, string>();

            _mappings[defaultHeaderName] = customHeaderName;

            _inverseMappings = _mappings.ToDictionary(pair => pair.Value, pair => pair.Key);

            Count = _mappings.Count;
        }

        public void Apply(IEnumerable<MessageHeader> headers) =>
            headers?.ForEach(
                header =>
                    header.Name = GetMappedHeaderName(header.Name, _mappings));

        public void Revert(IEnumerable<MessageHeader> headers) =>
            headers?.ForEach(
                header =>
                    header.Name = GetMappedHeaderName(header.Name, _inverseMappings));

        private static string GetMappedHeaderName(string headerName, Dictionary<string, string>? mappings)
        {
            if (mappings != null && mappings.TryGetValue(headerName, out string mappedHeaderName))
                return mappedHeaderName;

            return headerName;
        }
    }
}
