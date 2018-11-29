using System.Collections.Generic;

namespace Silverback
{
    /// <summary>
    /// Compare two Kafka clients configurations.
    /// </summary>
    internal class ConfigurationComparer : IEqualityComparer<Dictionary<string, object>>
    {
        public bool Equals(Dictionary<string, object> x, Dictionary<string, object> y)
        {
            return Compare(x, y);
        }

        public int GetHashCode(Dictionary<string, object> obj)
        {
            obj.TryGetValue("bootstrap.servers", out var serverAddress);
            var hashCodeReferer = $"{obj.Count}-{serverAddress}";

            unchecked
            {
                return obj.Count + hashCodeReferer.GetHashCode();
            }
        }

        public static bool Compare(Dictionary<string, object> x, Dictionary<string, object> y)
        {
            if (x == null || y == null) return false;
            if (x.Count != y.Count) return false;

            var valueComparer = EqualityComparer<object>.Default;

            foreach (var kvp in x)
            {
                if (!y.TryGetValue(kvp.Key, out var value2)) return false;
                if (value2 is Dictionary<string, object> val2
                    && kvp.Value is Dictionary<string, object> val1
                    && Compare(val2, val1)) continue;
                if (!valueComparer.Equals(kvp.Value, value2)) return false;
            }
            return true;
        }
    }
}
