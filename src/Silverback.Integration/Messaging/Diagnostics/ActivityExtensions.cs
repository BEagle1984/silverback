using System.Collections.Generic;
using System.Diagnostics;

namespace Silverback.Messaging.Diagnostics
{
    internal static class ActivityExtensions
    {
        // TODO: Test
        public static void AddBaggageRange(this Activity activity, IEnumerable<KeyValuePair<string, string>> baggageItems)
        {
            foreach (KeyValuePair<string, string> baggageItem in baggageItems)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }
        }
    }
}
