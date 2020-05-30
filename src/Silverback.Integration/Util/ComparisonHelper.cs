// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;

namespace Silverback.Util
{
    internal static class ComparisonHelper
    {
        /// <summary>
        ///     Determines whether the specified object instances are considered equal comparing their JSON
        ///     representations.
        /// </summary>
        /// <param name="objA">
        ///     The first of the two objects to be compared.
        /// </param>
        /// <param name="objB">
        ///     The second of the two objects to be compared.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the two objects serializes to the same JSON representation.
        /// </returns>
        public static bool JsonEquals(object? objA, object? objB) =>
            ReferenceEquals(objA, objB) ||
            objA != null &&
            objB != null &&
            Equals(GetJsonString(objA), GetJsonString(objB));

        private static string GetJsonString(object obj) =>
            JsonConvert.SerializeObject(
                obj,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
    }
}
